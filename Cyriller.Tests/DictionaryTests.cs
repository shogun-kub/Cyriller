using System;
using System.Collections.Generic;
using System.Linq;
using Cyriller.Model;
using Xunit;

namespace Cyriller.Tests
{
    public class DictionaryTests
    {
        private static readonly Lazy<CyrNounCollection> Nouns =
            new Lazy<CyrNounCollection>(() => new CyrNounCollection());

        private static readonly Lazy<CyrAdjectiveCollection> Adjectives =
            new Lazy<CyrAdjectiveCollection>(() => new CyrAdjectiveCollection());

        [Fact]
        public void KnownNounIsFoundWithItsGender()
        {
            CyrNoun noun = Nouns.Value.Get("кот", out CasesEnum foundCase, out NumbersEnum foundNumber, out GendersEnum gender);

            Assert.Equal(GendersEnum.Masculine, gender);
            Assert.Equal(AnimatesEnum.Animated, noun.Animate);
            Assert.Equal(CasesEnum.Nominative, foundCase);
            Assert.Equal(NumbersEnum.Singular, foundNumber);
            Assert.Equal(new[] { "кот", "кота", "коту", "кота", "котом", "коте" }, noun.Decline().ToArray());
        }

        [Fact]
        public void FeminineNounIsFoundWithItsGender()
        {
            CyrNoun noun = Nouns.Value.Get("кошка", out _, out _, out GendersEnum gender);

            Assert.Equal(GendersEnum.Feminine, gender);

            CyrResult singular = noun.Decline();

            Assert.Equal("кошки", singular.Genitive);
            Assert.Equal("кошке", singular.Dative);
            Assert.Equal("кошку", singular.Accusative);
            Assert.Equal("кошкой", singular.Instrumental);
            Assert.Equal("кошке", singular.Prepositional);
        }

        [Fact]
        public void UnknownNounIsDeclinedBySimilarDictionaryWord()
        {
            CyrNoun noun = Nouns.Value.Get("трипьютер", out string foundWord, out _, out _);

            Assert.NotNull(noun);
            Assert.Equal("компьютер", foundWord);
            Assert.Equal("трипьютера", noun.Decline().Genitive);
        }

        [Fact]
        public void KnownAdjectiveIsDeclinedInAllGenders()
        {
            CyrAdjective adjective = Adjectives.Value.Get("пушистая", out GendersEnum gender, out _, out _, out _);

            Assert.Equal(GendersEnum.Feminine, gender);
            Assert.Equal("пушистой", adjective.Decline(GendersEnum.Feminine, AnimatesEnum.Inanimated).Genitive);
            Assert.Equal("пушистого", adjective.Decline(GendersEnum.Masculine, AnimatesEnum.Animated).Genitive);
            Assert.Equal("пушистых", adjective.DeclinePlural(AnimatesEnum.Inanimated).Genitive);
        }

        [Fact]
        public void NumbersAreSpelledOutInTheRequestedCase()
        {
            CyrNumber number = new CyrNumber();

            Assert.Equal("двадцать один", number.ToString(21, CasesEnum.Nominative));
            Assert.Equal("двадцати одного", number.ToString(21, CasesEnum.Genitive));
            Assert.Equal("пять", number.ToString(5, CasesEnum.Nominative));
            Assert.Equal("пяти", number.ToString(5, CasesEnum.Genitive));
        }

        [Theory]
        [InlineData(1, "год")]
        [InlineData(2, "года")]
        [InlineData(5, "лет")]
        [InlineData(11, "лет")]
        [InlineData(21, "год")]
        public void UnitFormIsChosenByTheLastDigits(long value, string expected)
        {
            CyrNumber number = new CyrNumber();

            Assert.Equal(expected, number.Case(value, "год", "года", "лет"));
        }
    }
}
