using System;
using System.Collections.Generic;
using System.IO;
using Cyriller.Model;
using Xunit;

namespace Cyriller.Tests
{
    /// <summary>
    /// Сверка словарных форм с разметкой корпуса SynTagRus (Universal Dependencies, CC BY-NC-SA 4.0).
    /// Берём токены-существительные с падежом и числом, у которых лемма есть в словаре,
    /// и проверяем, что форма из корпуса получается склонением этой леммы.
    /// </summary>
    public class UdNounTests
    {
        private const int TokensToCheck = 4000;
        private const double MaxMismatchRate = 0.05;

        [Fact]
        public void NounFormsFromSynTagRusAreDeclined()
        {
            string path = CorpusFiles.Find("UD", "ru_syntagrus-ud-test.conllu");

            if (path == null)
            {
                return;
            }

            CyrNounCollection collection = CorpusFiles.Nouns.Value;
            List<string> mismatches = new List<string>();
            int checkedTokens = 0;
            int failed = 0;

            foreach (string line in File.ReadLines(path))
            {
                if (checkedTokens >= TokensToCheck)
                {
                    break;
                }

                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string[] columns = line.Split('\t');

                if (columns.Length < 6 || columns[3] != "NOUN")
                {
                    continue;
                }

                if (columns[0].Contains('-') || columns[0].Contains('.'))
                {
                    continue;
                }

                Dictionary<string, string> features = ParseFeatures(columns[5]);

                if (!features.TryGetValue("Case", out string caseName) || !features.TryGetValue("Number", out string numberName))
                {
                    continue;
                }

                CasesEnum? @case = ParseCase(caseName);
                NumbersEnum? number = numberName switch
                {
                    "Sing" => NumbersEnum.Singular,
                    "Plur" => NumbersEnum.Plural,
                    _ => null
                };

                if (@case == null || number == null)
                {
                    continue;
                }

                string form = columns[1];
                string lemma = columns[2];
                CyrNoun noun = collection.GetOrDefault(lemma, out _, out _);

                if (noun == null)
                {
                    continue;
                }

                CyrResult result = number == NumbersEnum.Singular ? noun.Decline() : noun.DeclinePlural();
                string actual = result.Get(@case.Value);

                checkedTokens++;

                if (CorpusFiles.Normalize(actual) == CorpusFiles.Normalize(form))
                {
                    continue;
                }

                failed++;

                if (mismatches.Count < 15)
                {
                    mismatches.Add($"{lemma} → {form} ({caseName}, {numberName}): словарь даёт \"{actual}\"");
                }
            }

            Assert.True(checkedTokens >= 500, $"Only {checkedTokens} dictionary nouns were checked in {path}.");

            double rate = (double)failed / checkedTokens;

            Assert.True(rate <= MaxMismatchRate, $"{failed} из {checkedTokens} форм расходятся со SynTagRus ({rate:P1}):{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}");
        }

        private static Dictionary<string, string> ParseFeatures(string value)
        {
            Dictionary<string, string> features = new Dictionary<string, string>();

            foreach (string part in value.Split('|'))
            {
                int index = part.IndexOf('=');

                if (index > 0)
                {
                    features[part.Substring(0, index)] = part.Substring(index + 1);
                }
            }

            return features;
        }

        private static CasesEnum? ParseCase(string name) => name switch
        {
            "Nom" => CasesEnum.Nominative,
            "Gen" => CasesEnum.Genitive,
            "Dat" => CasesEnum.Dative,
            "Acc" => CasesEnum.Accusative,
            "Ins" => CasesEnum.Instrumental,
            "Loc" => CasesEnum.Prepositional,
            _ => null
        };
    }
}
