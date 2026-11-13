using System;
using System.IO;
using System.Text;

namespace Cyriller.Tests
{
    /// <summary>
    /// Доступ к внешним корпусам в <c>Test_data</c>. Каталог не входит в репозиторий,
    /// поэтому тесты, которым он нужен, молча пропускаются, когда данных нет.
    /// </summary>
    internal static class CorpusFiles
    {
        /// <summary>
        /// Словарь существительных, общий для всех корпусных тестов: загрузка занимает
        /// несколько секунд, поэтому выполняется один раз на процесс.
        /// </summary>
        internal static readonly Lazy<CyrNounCollection> Nouns = new Lazy<CyrNounCollection>(() => new CyrNounCollection());

        /// <summary>
        /// Ищет путь внутри <c>Test_data</c>, поднимаясь от каталога сборки к корню репозитория.
        /// Возвращает <c>null</c>, если ничего не найдено.
        /// </summary>
        internal static string Find(params string[] relativeParts)
        {
            string[] tail = new string[relativeParts.Length + 1];
            tail[0] = "Test_data";
            Array.Copy(relativeParts, 0, tail, 1, relativeParts.Length);

            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, Path.Combine(tail));

                if (File.Exists(candidate) || Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }

        /// <summary>
        /// Приводит слово к виду, пригодному для сравнения: снимает знаки ударения,
        /// заменяет <c>ё</c> на <c>е</c> и переводит в нижний регистр.
        /// </summary>
        internal static string Normalize(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(word.Length);

            foreach (char c in word)
            {
                if (c == '\u0301' || c == '\u0300' || c == '\u0306' || c == '\u0308')
                {
                    continue;
                }

                builder.Append(c switch
                {
                    'ё' => 'е',
                    'Ё' => 'Е',
                    _ => c
                });
            }

            return builder.ToString().ToLowerInvariant();
        }
    }
}
