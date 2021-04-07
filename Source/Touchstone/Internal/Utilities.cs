using System;
using System.Collections.Generic;

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
        public static void ForEachParameter(int numberOfPorts, Action<(int OuterIndex, int InnerIndex)> action) 
        {
            for (int outer = 1; outer <= numberOfPorts; outer++)
            {
                for (int inner = 1; inner <= numberOfPorts; inner++)
                {
                    action((outer, inner));
                }
            }
        }
        public static IEnumerable<T> ForEachParameter<T>(int numberOfPorts, Func<(int OuterIndex, int InnerIndex), T> function)
        {
            for (int outer = 1; outer <= numberOfPorts; outer++)
            {
                for (int inner = 1; inner <= numberOfPorts; inner++)
                {
                    yield return function((outer, inner));
                }
            }
        }

    }
}
