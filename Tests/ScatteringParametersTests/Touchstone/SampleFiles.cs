using MicrowaveNetworks.Touchstone;
using System.Collections.Generic;

namespace MicrowaveNetworksTests.Touchstone
{
    public static class SampleFiles
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


            public const string OnePort =
@"# MHz S MA R 75
!freq magZ11 angZ11
100 0.99 -4
200 0.80 -22
300 0.707 -45
400 0.40 -62
500 0.01 -89";
            public const string FourPort =
@"! 4-port S-parameter data, taken at three frequency points
! note that data points need not be aligned
# GHz S MA R 50
5.00000 0.60 161.24 0.40 -42.20 0.42 -66.58 0.53 -79.34 !row 1
        0.40 -42.20 0.60 161.20 0.53 -79.34 0.42 -66.58 !row 2
        0.42 -66.58 0.53 -79.34 0.60 161.24 0.40 -42.20 !row 3
        0.53 -79.34 0.42 -66.58 0.40 -42.20 0.60 161.24 !row 4
6.00000 0.57 150.37 0.40 -44.34 0.41 -81.24 0.57 -95.77 !row 1
        0.40 -44.34 0.57 150.37 0.57 -95.77 0.41 -81.24 !row 2
        0.41 -81.24 0.57 -95.77 0.57 150.37 0.40 -44.34 !row 3
        0.57 -95.77 0.41 -81.24 0.40 -44.34 0.57 150.37 !row 4
7.00000 0.50 136.69 0.45 -46.41 0.37 -99.09 0.62 -114.19 !row 1
0.45 -46.41 0.50 136.69 0.62 -114.19 0.37 -99.09 !row 2
0.37 -99.09 0.62 -114.19 0.50 136.69 0.45 -46.41 !row 3
0.62 -114.19 0.37 -99.09 0.45 -46.41 0.50 136.69 !row 4";
        }
    }
}
