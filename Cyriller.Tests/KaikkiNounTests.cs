using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Cyriller.Model;
using Xunit;

namespace Cyriller.Tests
{
    /// <summary>
    /// Сверка словарных форм с таблицами склонения русских существительных из Wiktionary
    /// (сырой экстракт wiktextract, <c>Test_data/ru-extract.jsonl</c>).
    /// Файл очень большой, поэтому просматривается только начало и ограниченное число лемм.
    /// </summary>
    public class KaikkiNounTests
    {
        private const int LemmasToCheck = 1200;
        private const int LinesToScan = 400000;
        private const double MaxMismatchRate = 0.10;

        [Fact]
        public void NounFormsFromWiktionaryAreDeclined()
        {
            string path = CorpusFiles.Find("ru-extract.jsonl");

            if (path == null)
            {
                return;
            }

            CyrNounCollection collection = CorpusFiles.Nouns.Value;
            List<string> mismatches = new List<string>();
            HashSet<string> seenWords = new HashSet<string>(StringComparer.Ordinal);
            int checkedLemmas = 0;
            int failed = 0;
            int lines = 0;

            using FileStream stream = File.OpenRead(path);
            using StreamReader reader = new StreamReader(stream);

            while (reader.ReadLine() is string line)
            {
                if (checkedLemmas >= LemmasToCheck || ++lines > LinesToScan)
                {
                    break;
                }

                if (!line.Contains("\"pos\": \"noun\""))
                {
                    continue;
                }

                if (!TryReadExpectedForms(line, out string word, out Dictionary<(CasesEnum Case, NumbersEnum Number), HashSet<string>> expected))
                {
                    continue;
                }

                if (!seenWords.Add(word))
                {
                    continue;
                }

                CyrNoun noun = collection.GetOrDefault(word, out _, out _);

                if (noun == null)
                {
                    continue;
                }

                checkedLemmas++;

                string singularFailure = null;

                foreach (KeyValuePair<(CasesEnum Case, NumbersEnum Number), HashSet<string>> pair in expected)
                {
                    CyrResult result = pair.Key.Number == NumbersEnum.Singular ? noun.Decline() : noun.DeclinePlural();
                    string actual = CorpusFiles.Normalize(result.Get(pair.Key.Case));

                    if (pair.Value.Contains(actual))
                    {
                        continue;
                    }

                    if (mismatches.Count < 15)
                    {
                        mismatches.Add($"{word} ({pair.Key.Case}, {pair.Key.Number}): словарь даёт \"{actual}\", Wiktionary — {string.Join(" / ", pair.Value)}");
                    }

                    singularFailure = actual;
                    break;
                }

                if (singularFailure != null)
                {
                    failed++;
                }
            }

            Assert.True(checkedLemmas >= 300, $"Only {checkedLemmas} dictionary nouns were found in the first {lines} lines of {path}.");

            double rate = (double)failed / checkedLemmas;

            Assert.True(rate <= MaxMismatchRate, $"{failed} из {checkedLemmas} существительных расходятся с Wiktionary ({rate:P1}):{Environment.NewLine}{string.Join(Environment.NewLine, mismatches)}");
        }

        private static bool TryReadExpectedForms(string line, out string word, out Dictionary<(CasesEnum Case, NumbersEnum Number), HashSet<string>> expected)
        {
            word = null;
            expected = null;

            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("word", out JsonElement wordElement) || !root.TryGetProperty("forms", out JsonElement forms))
            {
                return false;
            }

            // Файл собран из русского Викисловаря и содержит статьи обо всех языках:
            // без этого фильтра в таблицы попадают украинские, татарские и церковнославянские формы.
            if (!root.TryGetProperty("lang_code", out JsonElement langCode) || langCode.GetString() != "ru")
            {
                return false;
            }

            word = wordElement.GetString();

            if (string.IsNullOrEmpty(word) || !IsPlainWord(word))
            {
                return false;
            }

            Dictionary<(CasesEnum, NumbersEnum), HashSet<string>> result = new Dictionary<(CasesEnum, NumbersEnum), HashSet<string>>();

            foreach (JsonElement form in forms.EnumerateArray())
            {
                if (!form.TryGetProperty("form", out JsonElement textElement) || !form.TryGetProperty("tags", out JsonElement tags))
                {
                    continue;
                }

                string text = textElement.GetString();

                if (string.IsNullOrEmpty(text) || !IsRussianForm(text))
                {
                    continue;
                }

                CasesEnum? @case = null;
                NumbersEnum? number = null;
                bool broken = false;

                foreach (JsonElement tag in tags.EnumerateArray())
                {
                    switch (tag.GetString())
                    {
                        case "nominative": @case = CasesEnum.Nominative; break;
                        case "genitive": @case = CasesEnum.Genitive; break;
                        case "dative": @case = CasesEnum.Dative; break;
                        case "accusative": @case = CasesEnum.Accusative; break;
                        case "instrumental": @case = CasesEnum.Instrumental; break;
                        case "prepositional": @case = CasesEnum.Prepositional; break;
                        case "singular": number = NumbersEnum.Singular; break;
                        case "plural": number = NumbersEnum.Plural; break;
                        case "error":
                        case "obsolete":
                        case "archaic":
                        case "dialectal":
                        case "nonstandard":
                        case "rare":
                        case "alternative":
                            broken = true;
                            break;
                    }
                }

                if (broken || @case == null || number == null)
                {
                    continue;
                }

                (CasesEnum, NumbersEnum) key = (@case.Value, number.Value);

                if (!result.TryGetValue(key, out HashSet<string> values))
                {
                    values = new HashSet<string>(StringComparer.Ordinal);
                    result.Add(key, values);
                }

                values.Add(CorpusFiles.Normalize(text));
            }

            if (result.Count == 0)
            {
                return false;
            }

            word = CorpusFiles.Normalize(word);
            expected = result;

            return true;
        }

        private static bool IsPlainWord(string word)
        {
            foreach (char c in word)
            {
                if (!char.IsLetter(c) && c != '-')
                {
                    return false;
                }
            }

            return word.Length > 1;
        }

        /// <summary>
        /// Отбрасывает пометки вроде <c>*бабы</c>, а также формы с буквами других языков
        /// (<c>і</c>, <c>ꙑ</c>, <c>ѣ</c> и т. п.), которые встречаются в таблицах Викисловаря.
        /// </summary>
        private static bool IsRussianForm(string text)
        {
            foreach (char c in text)
            {
                if (c == '\u0301' || c == '\u0300' || c == '\u0306' || c == '\u0308')
                {
                    continue;
                }

                char lower = char.ToLowerInvariant(c);

                if ((lower < 'а' || lower > 'я') && lower != 'ё' && lower != '-')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
