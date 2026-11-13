using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cyriller.Model;
using Xunit;

namespace Cyriller.Tests
{
    /// <summary>
    /// Выборочная проверка полных имён: берём 50 мужских и 50 женских пар
    /// «фамилия + имя» из <c>russian_surnames.csv</c> и <c>ru-pnames-list</c>,
    /// склоняем их и сверяем падежные формы с тем, как эти же слова просклоняны
    /// в корпусе SynTagRus (PROPN с <c>NameType=Sur</c> для фамилий и <c>Giv</c> для имён).
    /// Тип имени входит в ключ: одна и та же строка может быть и фамилией, и именем,
    /// и склоняются они по-разному (Марина → Марины как имя, Мариной как фамилия).
    /// </summary>
    public class NameDeclensionTests
    {
        private const int CombosPerGender = 50;
        private const double MaxMismatchRate = 0.10;

        private const string SurnameType = "Sur";
        private const string GivenNameType = "Giv";

        private static readonly CyrName CyrName = new CyrName();

        [Fact]
        public void SampledFullNamesMatchAttestedCorpusForms()
        {
            string csvPath = CorpusFiles.Find("russian_surnames.csv");
            string maleNamesPath = CorpusFiles.Find("ru-pnames-list", "lists", "male_names_rus.txt");
            string femaleNamesPath = CorpusFiles.Find("ru-pnames-list", "lists", "female_names_rus.txt");
            string conlluPath = CorpusFiles.Find("UD", "ru_syntagrus-ud-test.conllu");

            if (csvPath == null || maleNamesPath == null || femaleNamesPath == null || conlluPath == null)
            {
                return;
            }

            Dictionary<(string Lemma, GendersEnum Gender, string NameType, CasesEnum Case), HashSet<string>> attested = ReadAttestedForms(conlluPath);

            List<string> maleSurnames = ReadSurnames(csvPath, attested, GendersEnum.Masculine);
            List<string> femaleSurnames = ReadSurnames(csvPath, attested, GendersEnum.Feminine);
            List<string> maleNames = ReadNames(maleNamesPath, attested, GendersEnum.Masculine);
            List<string> femaleNames = ReadNames(femaleNamesPath, attested, GendersEnum.Feminine);

            Assert.True(maleSurnames.Count > 10 && femaleSurnames.Count > 10, "Not enough attested surnames in the corpus.");
            Assert.True(maleNames.Count > 10 && femaleNames.Count > 10, "Not enough attested given names in the corpus.");

            Random random = new Random(20261113);
            List<(string Surname, string Name, GendersEnum Gender)> combos = new List<(string, string, GendersEnum)>();

            combos.AddRange(Sample(maleSurnames, maleNames, GendersEnum.Masculine, CombosPerGender, random));
            combos.AddRange(Sample(femaleSurnames, femaleNames, GendersEnum.Feminine, CombosPerGender, random));

            List<string> mismatches = new List<string>();
            int componentChecks = 0;
            int failed = 0;

            foreach ((string surname, string name, GendersEnum gender) in combos)
            {
                CyrResult full = CyrName.Decline(surname, name, string.Empty, gender, false);

                Assert.Equal(surname + " " + name, full.Nominative);
                Assert.All(full.ToArray(), form => Assert.False(string.IsNullOrWhiteSpace(form)));

                CyrResult surnameResult = CyrName.Decline(surname, gender, false);

                Check("фамилия", SurnameType, surname, surnameResult, gender, attested, ref componentChecks, ref failed, mismatches);
                CheckName(name, gender, attested, ref componentChecks, ref failed, mismatches);
            }

            Assert.True(componentChecks >= 100, $"Only {componentChecks} case forms were attested in the corpus.");

            double rate = (double)failed / componentChecks;

            Assert.True(rate <= MaxMismatchRate, $"{failed} из {componentChecks} падежных форм расходятся с корпусом ({rate:P1}):{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}");
        }

        private static void Check(
            string kind,
            string nameType,
            string lemma,
            CyrResult result,
            GendersEnum gender,
            Dictionary<(string, GendersEnum, string, CasesEnum), HashSet<string>> attested,
            ref int checks,
            ref int failed,
            List<string> mismatches)
        {
            foreach (CasesEnum @case in Cases)
            {
                if (!TryGetForms(attested, lemma, gender, nameType, @case, out HashSet<string> forms))
                {
                    continue;
                }

                checks++;

                if (forms.Contains(CorpusFiles.Normalize(result.Get(@case))))
                {
                    continue;
                }

                failed++;

                if (mismatches.Count < 12)
                {
                    mismatches.Add($"{kind} {lemma} ({gender}, {@case}): библиотека даёт \"{result.Get(@case)}\", корпус — {string.Join(" / ", forms)}");
                }
            }
        }

        private static void CheckName(
            string name,
            GendersEnum gender,
            Dictionary<(string, GendersEnum, string, CasesEnum), HashSet<string>> attested,
            ref int checks,
            ref int failed,
            List<string> mismatches)
        {
            bool isFeminine = gender == GendersEnum.Feminine;

            foreach (CasesEnum @case in Cases)
            {
                if (!TryGetForms(attested, name, gender, GivenNameType, @case, out HashSet<string> forms))
                {
                    continue;
                }

                string actual = @case switch
                {
                    CasesEnum.Nominative => name,
                    CasesEnum.Genitive => CyrName.DeclineNameGenitive(name, isFeminine, false),
                    CasesEnum.Dative => CyrName.DeclineNameDative(name, isFeminine, false),
                    CasesEnum.Accusative => CyrName.DeclineNameAccusative(name, isFeminine, false),
                    CasesEnum.Instrumental => CyrName.DeclineNameInstrumental(name, isFeminine, false),
                    _ => CyrName.DeclineNamePrepositional(name, isFeminine, false)
                };

                checks++;

                if (forms.Contains(CorpusFiles.Normalize(actual)))
                {
                    continue;
                }

                failed++;

                if (mismatches.Count < 12)
                {
                    mismatches.Add($"имя {name} ({gender}, {@case}): библиотека даёт \"{actual}\", корпус — {string.Join(" / ", forms)}");
                }
            }
        }

        private static bool TryGetForms(
            Dictionary<(string, GendersEnum, string, CasesEnum), HashSet<string>> attested,
            string lemma,
            GendersEnum gender,
            string nameType,
            CasesEnum @case,
            out HashSet<string> forms)
        {
            return attested.TryGetValue((CorpusFiles.Normalize(lemma), gender, nameType, @case), out forms);
        }

        private static IEnumerable<CasesEnum> Cases => new[]
        {
            CasesEnum.Nominative,
            CasesEnum.Genitive,
            CasesEnum.Dative,
            CasesEnum.Accusative,
            CasesEnum.Instrumental,
            CasesEnum.Prepositional
        };

        private static List<(string Surname, string Name, GendersEnum Gender)> Sample(
            List<string> surnames,
            List<string> names,
            GendersEnum gender,
            int count,
            Random random)
        {
            List<(string, string, GendersEnum)> result = new List<(string, string, GendersEnum)>();

            for (int i = 0; i < count; i++)
            {
                string surname = surnames[random.Next(surnames.Count)];
                string name = names[random.Next(names.Count)];

                result.Add((surname, name, gender));
            }

            return result;
        }

        private static List<string> ReadSurnames(
            string path,
            Dictionary<(string, GendersEnum, string, CasesEnum), HashSet<string>> attested,
            GendersEnum gender)
        {
            List<string> result = new List<string>();

            foreach (string line in File.ReadLines(path))
            {
                string[] parts = line.Split(',');

                if (parts.Length != 3 || parts[0].Any(char.IsControl))
                {
                    continue;
                }

                if (parts[2] != (gender == GendersEnum.Feminine ? "f" : "m"))
                {
                    continue;
                }

                if (IsAttested(parts[0], gender, SurnameType, attested))
                {
                    result.Add(parts[0]);
                }
            }

            return result;
        }

        private static List<string> ReadNames(
            string path,
            Dictionary<(string, GendersEnum, string, CasesEnum), HashSet<string>> attested,
            GendersEnum gender)
        {
            List<string> result = new List<string>();

            foreach (string line in File.ReadLines(path))
            {
                string name = line.Trim();

                if (name.Length == 0 || name.Any(char.IsControl))
                {
                    continue;
                }

                if (IsAttested(name, gender, GivenNameType, attested))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        private static bool IsAttested(
            string lemma,
            GendersEnum gender,
            string nameType,
            Dictionary<(string, GendersEnum, string, CasesEnum), HashSet<string>> attested)
        {
            foreach (CasesEnum @case in Cases)
            {
                if (TryGetForms(attested, lemma, gender, nameType, @case, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<(string Lemma, GendersEnum Gender, string NameType, CasesEnum Case), HashSet<string>> ReadAttestedForms(string path)
        {
            Dictionary<(string, GendersEnum, string, CasesEnum), HashSet<string>> result = new Dictionary<(string, GendersEnum, string, CasesEnum), HashSet<string>>();

            foreach (string line in File.ReadLines(path))
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string[] columns = line.Split('\t');

                if (columns.Length < 6 || columns[3] != "PROPN")
                {
                    continue;
                }

                if (columns[0].Contains('-') || columns[0].Contains('.'))
                {
                    continue;
                }

                Dictionary<string, string> features = ParseFeatures(columns[5]);

                if (!features.TryGetValue("NameType", out string nameType) || (nameType != SurnameType && nameType != GivenNameType))
                {
                    continue;
                }

                if (!features.TryGetValue("Case", out string caseName))
                {
                    continue;
                }

                CasesEnum? @case = ParseCase(caseName);

                if (@case == null)
                {
                    continue;
                }

                GendersEnum gender = features.TryGetValue("Gender", out string genderName) && genderName == "Fem"
                    ? GendersEnum.Feminine
                    : GendersEnum.Masculine;

                (string, GendersEnum, string, CasesEnum) key = (CorpusFiles.Normalize(columns[2]), gender, nameType, @case.Value);

                if (!result.TryGetValue(key, out HashSet<string> forms))
                {
                    forms = new HashSet<string>(StringComparer.Ordinal);
                    result.Add(key, forms);
                }

                forms.Add(CorpusFiles.Normalize(columns[1]));
            }

            return result;
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
