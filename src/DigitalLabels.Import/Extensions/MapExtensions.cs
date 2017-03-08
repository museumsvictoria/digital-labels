using System.Collections.Generic;
using System.Linq;
using IMu;

namespace DigitalLabels.Import.Extensions
{
    public static class MapExtensions
    {
        public static string GetTrimString(this Map map, string input)
        {
            return TrimString(map.GetString(input));
        }

        public static IList<string> GetTrimStrings(this Map map, string input)
        {
            var mapStrings = map.GetStrings(input);

            if (mapStrings != null && mapStrings.Any())
                return mapStrings
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(TrimString)
                    .ToList();

            return new List<string>();
        }

        public static string GetCleanString(this Map map, string input)
        {
            return map.GetString(input).RemoveNonWordCharacters();
        }

        private static string TrimString(string input)
        {
            return !string.IsNullOrWhiteSpace(input) ? input.Trim() : input;
        }
    }
}
