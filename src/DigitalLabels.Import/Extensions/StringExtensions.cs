using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DigitalLabels.Import.Extensions
{
    public static class StringExtensions
    {
        public static string Concatenate(this IEnumerable<string> input, string delimiter)
        {
            var s = new StringBuilder();

            if (input != null)
            {
                foreach (var item in input)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        if (s.Length != 0)
                            s.Append(delimiter);

                        s.Append(item);
                    }
                }
            }

            return s.Length != 0 ? s.ToString() : null;
        }

        public static string ReplaceLineBreaks(this string input, string delimiter = " ")
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return Regex
                .Replace(input, @"\r\n?|\n", delimiter)
                .Trim();
        }

        public static string RemoveNonWordCharacters(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            return Regex.Replace(input, @"[^\w\s]", string.Empty);
        }
    }

}
