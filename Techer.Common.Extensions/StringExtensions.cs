using System.Globalization;
using System.Text;

namespace Techer.Common.Extensions
{
    public static class StringExtensions
    {
        public static string LimitLength2(this string value, int limit, string suffix = "..")
        {
            if (value == null)
                return string.Empty;

            if (value.Length > limit)
                return string.Concat(value.AsSpan(0, limit - suffix.Length), suffix);

            return value;
        }

        public static string? LimitLength(this string? value, int limit, string suffix = "..")
        {
            if (value == null)
                return null;

            if (value.Length > limit)
                return string.Concat(value.AsSpan(0, limit - suffix.Length), suffix);

            return value;
        }

        public static string GetNumbers(this string value)
        {
            var tmp = string.Empty;

            if (!string.IsNullOrEmpty(value))
            {
                for (var i = 0; i < value.Length; i++)
                    if (char.IsDigit(value[i]))
                        tmp += value[i];
            }

            return tmp;
        }

        public static string ToFormat(this string value, string mask)
        {
            string res = string.Empty;

            if (!string.IsNullOrEmpty(value))
            {
                int p = 0;

                for (int i = 0; i < mask.Length && p < value.Length; i++)
                {
                    if (mask[i] == '#')
                        res += value[p++];
                    else
                        res += mask[i];
                }
            }

            return res;
        }

        public static string ToBase64(this string s)
        {
            if (s == null)
                return null;

            var bytes = Encoding.UTF8.GetBytes(s);
            return Convert.ToBase64String(bytes);
        }

        public static string FromBase64(this string s)
        {
            if (s == null)
                return null;

            var bytes = Convert.FromBase64String(s);

            return Encoding.UTF8.GetString(bytes);
        }

        public static string RemoverAcentos(this string texto)
        {
            string s = texto.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < s.Length; k++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(s[k]);

                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(s[k]);
            }

            return sb.ToString();
        }

        public static string ToTitleCase(this string s)
        {
            if (s == null)
                return null;

            var textInfo = new CultureInfo("pt-BR", false).TextInfo;

            return textInfo.ToTitleCase(s.ToLower());
        }

        public static System.String CutStart(this System.String s, System.String what)
        {
            if (s.StartsWith(what))
                return s.Substring(what.Length);
            else
                return s;
        }

    }
}
