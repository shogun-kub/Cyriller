using System;
using System.IO;
using System.IO.Compression;
using Xunit;

namespace Cyriller.Tests
{
    /// <summary>
    /// Сборка больше не упаковывает словари (это ломало параллельную сборку решения), поэтому
    /// проверяем, что отслеживаемые <c>App_Data/*.gz</c> не отстали от <c>*.txt</c>.
    /// </summary>
    public class EmbeddedDictionaryTests
    {
        [Theory]
        [InlineData("nouns")]
        [InlineData("adjectives")]
        public void PackedDictionaryMatchesTextFile(string name)
        {
            string textPath = FindAppData(name + ".txt");
            string packedPath = FindAppData(name + ".gz");

            if (textPath == null || packedPath == null)
            {
                return;
            }

            byte[] text = File.ReadAllBytes(textPath);
            byte[] packed = Decompress(packedPath);

            Assert.Equal(text.Length, packed.Length);
            Assert.Equal(text, packed);
        }

        private static byte[] Decompress(string path)
        {
            using FileStream file = File.OpenRead(path);
            using GZipStream gzip = new GZipStream(file, CompressionMode.Decompress);
            using MemoryStream memory = new MemoryStream();
            gzip.CopyTo(memory);
            return memory.ToArray();
        }

        private static string FindAppData(string fileName)
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Cyriller", "App_Data", fileName);

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
