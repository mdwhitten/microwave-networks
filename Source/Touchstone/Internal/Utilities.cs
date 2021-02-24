using System;

namespace Touchstone.Internal
{
    static class Utilities
    {
        public static bool IsPerfectSquare(this int input) => input.IsPerfectSquare(out _);
        public static bool IsPerfectSquare(this int input, out int root)
        {
            double sqrt = Math.Sqrt(input);
            root = (int)sqrt;
            return input != 0 && sqrt % 1 <= double.Epsilon;
        }

    }
}
