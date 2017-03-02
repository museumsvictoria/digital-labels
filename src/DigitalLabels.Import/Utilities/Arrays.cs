using System.Linq;

namespace DigitalLabels.Import.Utilities
{
    public static class Arrays
    {
        public static int FindLongestLength(params string[][] lists)
        {
            return lists?.Max(x => x.Length) ?? -1;
        }
    }
}
