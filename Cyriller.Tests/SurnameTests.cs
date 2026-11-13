using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cyriller.Model;
using Xunit;

namespace Cyriller.Tests
{
    /// <summary>
    /// Проверки склонения фамилий. Часть проверок использует внешний набор данных
    /// <c>Test_data/russian_surnames.csv</c>: он не входит в репозиторий, поэтому такие
    /// тесты молча пропускаются, когда файла нет.
    /// </summary>
    public class SurnameTests
    {
        private static readonly CyrName CyrName = new CyrName();

        [Fact]
        public void MasculineSurnameIsDeclined()
        {
            CyrResult result = CyrName.Decline("Иванов", GendersEnum.Masculine, false);

            Assert.Equal("Иванова", result.Genitive);
            Assert.Equal("Иванову", result.Dative);
            Assert.Equal("Иванова", result.Accusative);
            Assert.Equal("Ивановым", result.Instrumental);
            Assert.Equal("Иванове", result.Prepositional);
        }

        [Fact]
        public void FeminineSurnameIsDeclined()
        {
            CyrResult result = CyrName.Decline("Иванова", GendersEnum.Feminine, false);

            Assert.Equal("Ивановой", result.Genitive);
            Assert.Equal("Ивановой", result.Dative);
            Assert.Equal("Иванову", result.Accusative);
            Assert.Equal("Ивановой", result.Instrumental);
            Assert.Equal("Ивановой", result.Prepositional);
        }

        [Fact]
        public void HyphenatedSurnameIsDeclinedPartByPart()
        {
            CyrResult result = CyrName.Decline("Петров-Водкин", GendersEnum.Masculine, false);

            Assert.Equal("Петрова-Водкина", result.Genitive);
        }

        [Fact]
        public void SurnameSampleFromTestDataIsDeclinedWithoutFailures()
        {
            string file = FindSurnameFile();

            if (file == null)
            {
                return;
            }

            int checkedRows = 0;

            foreach (string line in File.ReadLines(file, Encoding.UTF8).Skip(1))
            {
                string[] parts = line.Split(',');

                if (parts.Length != 3 || parts[0].Any(char.IsControl))
                {
                    continue;
                }

                checkedRows++;

                if (checkedRows % 41 != 0)
                {
                    continue;
                }

                GendersEnum gender = parts[2] switch
                {
                    "m" => GendersEnum.Masculine,
                    "f" => GendersEnum.Feminine,
                    _ => GendersEnum.Undefined
                };

                CyrResult result = CyrName.Decline(parts[0], gender, false);

                Assert.Equal(parts[0], result.Nominative);
                Assert.All(result.ToArray(), form => Assert.False(string.IsNullOrWhiteSpace(form)));
            }

            Assert.True(checkedRows > 1000, $"Only {checkedRows} rows were read from {file}.");
        }

        private static string FindSurnameFile()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Test_data", "russian_surnames.csv");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
