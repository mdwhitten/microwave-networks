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
            switch (p)
            {
                case ParameterType.Scattering:
                    return typeof(ScatteringParametersMatrix);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
