using System.Collections.Generic;

namespace MicrowaveNetworks.Touchstone.Internal
{
    internal enum TouchstoneKeywords
    {
        [TouchstoneParameter("Version")]
        Version,
        [TouchstoneParameter("Number of Ports")]
        NumberOfPorts,
        [TouchstoneParameter("Two-Port Data Order")]
        TwoPortDataOrder,
        [TouchstoneParameter("Number Of Frequencies")]
        NumberOfFrequencies,
        [TouchstoneParameter("Number of Noise Frequencies")]
        NumberOfNoiseFrequencies,
        [TouchstoneParameter("Reference")]
        Reference,
        [TouchstoneParameter("Matrix Format")]
        MatrixFormat,
        [TouchstoneParameter("Mixed-Mode Order")]
        MixedModeOrder,
        [TouchstoneParameter("Begin Information")]
        BeginInformation,
        [TouchstoneParameter("End Information")]
        EndInformation,
        [TouchstoneParameter("Network Data")]
        NetworkData,
        [TouchstoneParameter("Noise Data")]
        NoiseData,
        [TouchstoneParameter("End")]
        End
    }
}
