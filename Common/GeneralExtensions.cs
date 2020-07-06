using System.Collections.Generic;

namespace Common
{
    public static class GeneralExtensions
    {
        public static IEnumerable<TItem> AsEnumerable<TItem>(this TItem item)
        {
            return new[] {item};
        }
    }
}