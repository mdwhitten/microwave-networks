using MicrowaveNetworks.Touchstone;
using System.Collections.Generic;

namespace MicrowaveNetworksTests.TouchstoneTests
{
    public static class TestCases
    {
        public static class V1
        {
            public static List<(string, TouchstoneOptions)> HeaderMaps = new List<(string, TouchstoneOptions)>()
            {
                ("# MHz S MA R 75", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.MHz,
                    Parameter = ParameterType.Scattering,
                    Resistance = 75,
                    Format = FormatType.MagnitudeAngle
                }),
                // Validates spacing with one or more whitespace characters
                ("#   MHz   S  MA R\t75", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.MHz,
                    Parameter = ParameterType.Scattering,
                    Resistance = 75,
                    Format = FormatType.MagnitudeAngle
                }),
                ("# R 50 Y Hz DB", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.Hz,
                    Parameter = ParameterType.Admittance,
                    Resistance = 50,
                    Format = FormatType.DecibelAngle
                }),
                ("# G R 50 GHz RI", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.GHz,
                    Parameter = ParameterType.HybridG,
                    Resistance = 50,
                    Format = FormatType.RealImaginary
                }),
                ("# R 75", new TouchstoneOptions
                {
                    Resistance = 75,
                }),
                ("#", new TouchstoneOptions()),
            };
        }
    }
}
