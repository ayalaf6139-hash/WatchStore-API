using System.Text;

namespace WatchStore.Core.Common
{
    public static class PhoneNumber
    {
        public const int MaxLength = 16;

        private const string IsraelCountryCode = "972";

        public static bool TryNormalize(string? input, out string normalized)
        {
            normalized = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var trimmed = input.Trim();
            var hasPlus = trimmed[0] == '+';
            var digits = new StringBuilder(trimmed.Length);

            for (var i = 0; i < trimmed.Length; i++)
            {
                var ch = trimmed[i];

                if (char.IsAsciiDigit(ch))
                    digits.Append(ch);
                else if (ch is ' ' or '-' or '(' or ')' or '.')
                    continue;
                else if (ch == '+' && i == 0)
                    continue;
                else
                    return false;
            }

            var number = digits.ToString();
            string international;

            if (hasPlus)
                international = number;
            else if (number.StartsWith("00"))
                international = number[2..];
            else if (number.StartsWith(IsraelCountryCode) && number.Length is 11 or 12)
                international = number;
            else if (number.StartsWith('0') && number.Length is 9 or 10)
                international = IsraelCountryCode + number[1..];
            else
                return false;

            if (international.Length is < 8 or > 15 || international[0] == '0')
                return false;

            if (international.StartsWith(IsraelCountryCode) &&
                !IsValidIsraeliNationalNumber(international[IsraelCountryCode.Length..]))
                return false;

            normalized = "+" + international;
            return true;
        }

        private static bool IsValidIsraeliNationalNumber(string national)
        {
            return national.Length is 8 or 9 && national[0] != '0';
        }
    }
}
