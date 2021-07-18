namespace Sgml
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class Extensions
    {
        public static bool EqualsIgnoreCase(this string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        // Bitwise operators on enums to check for flags are much faster than Enum.HasFlag(), unfortunately.

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool HasFlagBits(this TextWhitespaceHandling value, TextWhitespaceHandling bits) => (value & bits) == bits;
    }
}
