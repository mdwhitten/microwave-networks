using System;
using System.Collections.Generic;
using MicrowaveNetworks.Matrices;
using MicrowaveNetworks.Touchstone;

namespace MicrowaveNetworks.Internal
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
        public static void ForEachParameter(int numberOfPorts, Action<(int DestinationPort, int SourcePort)> action)
        {
            ForEachParameter(numberOfPorts, ListFormat.SourcePortMajor, action);
        }
        public static void ForEachParameter(int numberOfPorts, ListFormat format, Action<(int DestinationPort, int SourcePort)> action)
        {
            for (int outer = 1; outer <= numberOfPorts; outer++)
            {
                for (int inner = 1; inner <= numberOfPorts; inner++)
                {
                    if (format == ListFormat.SourcePortMajor)
                    {
                        action((inner, outer));
                    }
                    else action((outer, inner));
                }
            }
        }
        public static IEnumerable<T> ForEachParameter<T>(int numberOfPorts, Func<(int DestinationPort, int SourcePort), T> function)
        {
            return ForEachParameter(numberOfPorts, ListFormat.SourcePortMajor, function);
        }
        public static IEnumerable<T> ForEachParameter<T>(int numberOfPorts, ListFormat format, Func<(int DestinationPort, int SourcePort), T> function)
        {
            for (int outer = 1; outer <= numberOfPorts; outer++)
            {
                for (int inner = 1; inner <= numberOfPorts; inner++)
                {
                    if (format == ListFormat.SourcePortMajor)
                    {
                        yield return function((inner, outer));
                    }
                    else yield return function((outer, inner));
                }
            }
        }
        public static Type ToNetworkParameterMatrixType(this ParameterType p)
        {
            return p switch
            {
                ParameterType.Scattering => typeof(ScatteringParametersMatrix),
                _ => throw new NotImplementedException($"Support for parameter type {p} has not been implemented."),
            };
        }


        #region IList<T> Binary Helpers

        // Below copied from https://stackoverflow.com/a/2948872

        /// <summary>
        /// Performs a binary search on the specified collection.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <typeparam name="TSearch">The type of the searched item.</typeparam>
        /// <param name="list">The list to be searched.</param>
        /// <param name="value">The value to search for.</param>
        /// <param name="comparer">The comparer that is used to compare the value
        /// with the list items.</param>
        /// <returns></returns>
        public static int BinarySearch<TItem, TSearch>(this IList<TItem> list,
            TSearch value, Func<TSearch, TItem, int> comparer)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }
            if (comparer == null)
            {
                throw new ArgumentNullException("comparer");
            }

            int lower = 0;
            int upper = list.Count - 1;

            while (lower <= upper)
            {
                int middle = lower + (upper - lower) / 2;
                int comparisonResult = comparer(value, list[middle]);
                if (comparisonResult < 0)
                {
                    upper = middle - 1;
                }
                else if (comparisonResult > 0)
                {
                    lower = middle + 1;
                }
                else
                {
                    return middle;
                }
            }

            return ~lower;
        }

        /// <summary>
        /// Performs a binary search on the specified collection.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="list">The list to be searched.</param>
        /// <param name="value">The value to search for.</param>
        /// <returns></returns>
        public static int BinarySearch<TItem>(this IList<TItem> list, TItem value)
        {
            return BinarySearch(list, value, Comparer<TItem>.Default);
        }

        /// <summary>
        /// Performs a binary search on the specified collection.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="list">The list to be searched.</param>
        /// <param name="value">The value to search for.</param>
        /// <param name="comparer">The comparer that is used to compare the value
        /// with the list items.</param>
        /// <returns></returns>
        public static int BinarySearch<TItem>(this IList<TItem> list, TItem value,
            IComparer<TItem> comparer)
        {
            return list.BinarySearch(value, comparer.Compare);
        }
        #endregion
    }
}
