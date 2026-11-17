using WatchStore.Core.Common;

namespace WatchStore.Tests.Domain
{
    public class PhoneNumberTests
    {
        [Theory]
        [InlineData("050-1234567", "+972501234567")]
        [InlineData("0501234567", "+972501234567")]
        [InlineData("050 123 4567", "+972501234567")]
        [InlineData("+972 50-123-4567", "+972501234567")]
        [InlineData("00972501234567", "+972501234567")]
        [InlineData("972501234567", "+972501234567")]
        [InlineData("02-6543210", "+97226543210")]
        [InlineData("(03) 555-1234", "+97235551234")]
        [InlineData("+1 (212) 555-0100", "+12125550100")]
        public void TryNormalize_ValidNumbers_ReturnsE164(string input, string expected)
        {
            var result = PhoneNumber.TryNormalize(input, out var normalized);

            Assert.True(result);
            Assert.Equal(expected, normalized);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc")]
        [InlineData("12345")]
        [InlineData("050-123")]
        [InlineData("050*1234567")]
        [InlineData("+972 0501234567")]
        [InlineData("05+01234567")]
        [InlineData("+12345678901234567")]
        public void TryNormalize_InvalidNumbers_ReturnsFalse(string? input)
        {
            var result = PhoneNumber.TryNormalize(input, out var normalized);

            Assert.False(result);
            Assert.Equal(string.Empty, normalized);
        }
    }
}
