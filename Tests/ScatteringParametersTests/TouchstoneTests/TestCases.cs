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
                // Standard complete header
                ("# MHz S MA R 75", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.MHz,
                    Parameter = ParameterType.Scattering,
                    Resistance = 75,
                    Reactance = 0,
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
                // Different ordering #1
                ("# R 50 Y Hz DB", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.Hz,
                    Parameter = ParameterType.Admittance,
                    Resistance = 50,
                    Reactance = 0,
                    Format = FormatType.DecibelAngle
                }),
                // Different ordering #2
                ("# G R 50 GHz RI", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.GHz,
                    Parameter = ParameterType.HybridG,
                    Resistance = 50,
                    Reactance = 0,
                    Format = FormatType.RealImaginary
                }),
                // Missing some values (valid per spec)
                ("# R 75", new TouchstoneOptions
                {
                    Resistance = 75,
                    Reactance = 0,
                }),
                // Missing all values (valid per spec)
                ("#", new TouchstoneOptions()),
                // Capitalization that deviates from standard capitalized values
                ("# hz s dB r 50", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.Hz,
                    Parameter = ParameterType.Scattering,
                    Resistance = 50,
                    Reactance = 0,
                    Format = FormatType.DecibelAngle,
                }),
                // Standard complete header complex resistance
                ("# MHz S MA R (75-20j)", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.MHz,
                    Parameter = ParameterType.Scattering,
                    Resistance = 75,
                    Reactance = -20,
                    Format = FormatType.MagnitudeAngle
                }),
                // Standard complete header complex resistance
                ("# MHz S MA R (75+20j)", new TouchstoneOptions
                {
                    FrequencyUnit = FrequencyUnit.MHz,
                    Parameter = ParameterType.Scattering,
                    Resistance = 75,
                    Reactance = 20,
                    Format = FormatType.MagnitudeAngle
                }),
            };
        }
    }
}
