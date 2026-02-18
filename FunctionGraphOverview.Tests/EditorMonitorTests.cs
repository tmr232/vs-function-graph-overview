using Xunit;

namespace FunctionGraphOverview.Tests
{
    public class LanguageMapTests
    {
        [Theory]
        [InlineData(".c", "C")]
        [InlineData(".cpp", "C++")]
        [InlineData(".cxx", "C++")]
        [InlineData(".cc", "C++")]
        [InlineData(".h", "C++")]
        [InlineData(".hpp", "C++")]
        [InlineData(".go", "Go")]
        [InlineData(".java", "Java")]
        [InlineData(".py", "Python")]
        [InlineData(".ts", "TypeScript")]
        [InlineData(".tsx", "TSX")]
        public void TryGetLanguage_ContainsExpectedMapping(
            string extension,
            string expectedLanguage
        )
        {
            Assert.True(
                LanguageMap.TryGetLanguage(extension, out var language),
                $"Missing extension: {extension}"
            );
            Assert.Equal(expectedLanguage, language);
        }

        [Theory]
        [InlineData(".C")]
        [InlineData(".CPP")]
        [InlineData(".Py")]
        public void TryGetLanguage_IsCaseInsensitive(string extension)
        {
            Assert.True(
                LanguageMap.TryGetLanguage(extension, out _),
                $"Map should match case-insensitively: {extension}"
            );
        }

        [Theory]
        [InlineData(".js")]
        [InlineData(".rb")]
        [InlineData(".rs")]
        [InlineData(".txt")]
        public void TryGetLanguage_ReturnsFalseForUnsupportedExtensions(string extension)
        {
            Assert.False(
                LanguageMap.TryGetLanguage(extension, out _),
                $"Unexpected extension: {extension}"
            );
        }
    }
}
