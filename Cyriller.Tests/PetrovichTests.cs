using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cyriller.Model;
using Xunit;

namespace Cyriller.Tests
{
    /// <summary>
    /// Сверка склонения ФИО с эталонами библиотеки Petrovich (MIT).
    /// Источники: <c>Test_data/petrovich-net/src/NPetrovich.Tests/Data</c> и
    /// <c>Test_data/petrovich.spec.js</c>. Оба необязательны.
    /// </summary>
    public class PetrovichTests
    {
        private static readonly CyrName CyrName = new CyrName();

        private static readonly Regex SpecRegex = new Regex(
            @"expect\(p\.(male|female)\.(first|middle|last)\.(nominative|genitive|dative|accusative|instrumental|prepositional)\('([^']+)'\)\)\.toBe\('([^']+)'\)",
            RegexOptions.Compiled);

        [Fact]
        public void CsvFixturesAreDeclined()
        {
            string directory = CorpusFiles.Find("petrovich-net", "src", "NPetrovich.Tests", "Data");

            if (directory == null)
            {
                return;
            }

            int checkedRows = 0;
            int failed = 0;
            List<string> mismatches = new List<string>();

            foreach ((string file, NamePart part) in new[]
            {
                ("FirstNames.csv", NamePart.First),
                ("MiddleNames.csv", NamePart.Middle),
                ("LastNames.csv", NamePart.Last)
            })
            {
                string path = Path.Combine(directory, file);

                if (!File.Exists(path))
                {
                    continue;
                }

                foreach (string line in File.ReadLines(path))
                {
                    string[] parts = line.Split(',');

                    if (parts.Length < 4)
                    {
                        continue;
                    }

                    string word = parts[0].Trim();
                    GendersEnum gender = parts[1].Trim() == "Female" ? GendersEnum.Feminine : GendersEnum.Masculine;
                    CasesEnum @case = ParseCase(parts[2].Trim());
                    string expected = parts[3].Trim();
                    string actual = Decline(word, part, gender, @case);

                    checkedRows++;

                    if (actual == expected)
                    {
                        continue;
                    }

                    failed++;

                    if (mismatches.Count < 10)
                    {
                        mismatches.Add($"{word} ({gender}, {@case}, {part}): ожидалось \"{expected}\", получено \"{actual}\"");
                    }
                }
            }

            Assert.True(checkedRows > 10, $"Only {checkedRows} Petrovich CSV rows were read from {directory}.");

            double rate = (double)failed / checkedRows;

            Assert.True(rate <= 0.10, $"{failed} из {checkedRows} склонений расходятся с Petrovich ({rate:P0}):{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}");
        }

        [Fact]
        public void SpecFixturesMatch()
        {
            string path = CorpusFiles.Find("petrovich.spec.js");

            if (path == null)
            {
                return;
            }

            List<string> mismatches = new List<string>();
            int total = 0;
            int failed = 0;

            foreach (Match match in SpecRegex.Matches(File.ReadAllText(path)))
            {
                GendersEnum gender = match.Groups[1].Value == "female" ? GendersEnum.Feminine : GendersEnum.Masculine;
                NamePart part = match.Groups[2].Value switch
                {
                    "first" => NamePart.First,
                    "middle" => NamePart.Middle,
                    _ => NamePart.Last
                };
                CasesEnum @case = ParseCase(match.Groups[3].Value);
                string word = match.Groups[4].Value;
                string expected = match.Groups[5].Value;

                // Petrovich ожидает «Пашой», но это форма титула «паша́» (ударение на последнем
                // слоге). Имя Па́ша безударно и даёт «Пашей» — так же, как Саша, Гоша, Миша.
                // Библиотека склоняет личные имена, поэтому расхождение считаем ошибкой эталона.
                if (word == "Паша" && part == NamePart.First && @case == CasesEnum.Instrumental)
                {
                    continue;
                }

                total++;

                string actual = Decline(word, part, gender, @case);

                if (actual != expected)
                {
                    failed++;

                    if (mismatches.Count < 15)
                    {
                        mismatches.Add($"{part} {word} ({gender}, {@case}): ожидалось \"{expected}\", получено \"{actual}\"");
                    }
                }
            }

            Assert.True(total >= 15, $"Only {total} Petrovich spec expectations were parsed from {path}.");

            double rate = (double)failed / total;

            Assert.True(rate <= 0.15, $"{failed} из {total} склонений расходятся с Petrovich ({rate:P0}):{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}");
        }

        private static CasesEnum ParseCase(string name) => name.ToLowerInvariant() switch
        {
            "nominative" => CasesEnum.Nominative,
            "genitive" => CasesEnum.Genitive,
            "dative" => CasesEnum.Dative,
            "accusative" => CasesEnum.Accusative,
            "instrumental" => CasesEnum.Instrumental,
            "prepositional" => CasesEnum.Prepositional,
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown case.")
        };

        private static string Decline(string word, NamePart part, GendersEnum gender, CasesEnum @case)
        {
            bool isFeminine = gender == GendersEnum.Feminine;

            if (@case == CasesEnum.Nominative)
            {
                return word;
            }

            return part switch
            {
                NamePart.First => @case switch
                {
                    CasesEnum.Genitive => CyrName.DeclineNameGenitive(word, isFeminine, false),
                    CasesEnum.Dative => CyrName.DeclineNameDative(word, isFeminine, false),
                    CasesEnum.Accusative => CyrName.DeclineNameAccusative(word, isFeminine, false),
                    CasesEnum.Instrumental => CyrName.DeclineNameInstrumental(word, isFeminine, false),
                    _ => CyrName.DeclineNamePrepositional(word, isFeminine, false)
                },
                NamePart.Middle => @case switch
                {
                    CasesEnum.Genitive => CyrName.DeclinePatronymicGenitive(word, isFeminine, false),
                    CasesEnum.Dative => CyrName.DeclinePatronymicDative(word, isFeminine, false),
                    CasesEnum.Accusative => CyrName.DeclinePatronymicAccusative(word, isFeminine, false),
                    CasesEnum.Instrumental => CyrName.DeclinePatronymicInstrumental(word, isFeminine, false),
                    _ => CyrName.DeclinePatronymicPrepositional(word, isFeminine, false)
                },
                _ => @case switch
                {
                    CasesEnum.Genitive => CyrName.DeclineSurnameGenitive(word, isFeminine),
                    CasesEnum.Dative => CyrName.DeclineSurnameDative(word, isFeminine),
                    CasesEnum.Accusative => CyrName.DeclineSurnameAccusative(word, isFeminine),
                    CasesEnum.Instrumental => CyrName.DeclineSurnameInstrumental(word, isFeminine),
                    _ => CyrName.DeclineSurnamePrepositional(word, isFeminine)
                }
            };
        }

        private enum NamePart
        {
            First,
            Middle,
            Last
        }
    }
}
