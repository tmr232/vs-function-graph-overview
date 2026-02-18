using Xunit;

namespace FunctionGraphOverview.Tests
{
    public class NavigationServiceTests
    {
        [Fact]
        public void Utf8ByteOffsetToCharOffset_AsciiOnly_MatchesCharIndex()
        {
            var text = "hello world";
            // 'w' starts at byte 6 and char 6 for pure ASCII.
            Assert.Equal(6, NavigationService.Utf8ByteOffsetToCharOffset(text, 6));
        }

        [Fact]
        public void Utf8ByteOffsetToCharOffset_ZeroOffset_ReturnsZero()
        {
            Assert.Equal(0, NavigationService.Utf8ByteOffsetToCharOffset("abc", 0));
        }

        [Fact]
        public void Utf8ByteOffsetToCharOffset_EndOfString_ReturnsLength()
        {
            var text = "abc";
            Assert.Equal(3, NavigationService.Utf8ByteOffsetToCharOffset(text, 3));
        }

        [Fact]
        public void Utf8ByteOffsetToCharOffset_MultiByte_Utf8()
        {
            // 'é' is 2 bytes in UTF-8 but 1 char in UTF-16.
            var text = "café";
            // c(1) a(1) f(1) é(2) = 5 bytes total.
            // At byte offset 3 we've consumed "caf" → 3 chars.
            Assert.Equal(3, NavigationService.Utf8ByteOffsetToCharOffset(text, 3));
            // At byte offset 5 (end) we've consumed "café" → 4 chars.
            Assert.Equal(4, NavigationService.Utf8ByteOffsetToCharOffset(text, 5));
        }

        [Fact]
        public void Utf8ByteOffsetToCharOffset_Emoji_SurrogatePair()
        {
            // '😀' is 4 bytes in UTF-8 and 2 chars (surrogate pair) in UTF-16.
            var text = "a😀b";
            // a(1) 😀(4) b(1) = 6 bytes.
            // Byte 1 → 1 char ('a').
            Assert.Equal(1, NavigationService.Utf8ByteOffsetToCharOffset(text, 1));
            // Byte 5 → 3 chars ('a' + surrogate pair).
            Assert.Equal(3, NavigationService.Utf8ByteOffsetToCharOffset(text, 5));
            // Byte 6 → 4 chars.
            Assert.Equal(4, NavigationService.Utf8ByteOffsetToCharOffset(text, 6));
        }

        [Fact]
        public void Utf8ByteOffsetToCharOffset_NegativeOffset_ReturnsMinusOne()
        {
            Assert.Equal(-1, NavigationService.Utf8ByteOffsetToCharOffset("abc", -1));
        }

        [Fact]
        public void Utf8ByteOffsetToCharOffset_OffsetBeyondLength_ReturnsMinusOne()
        {
            Assert.Equal(-1, NavigationService.Utf8ByteOffsetToCharOffset("abc", 4));
        }
    }
}
