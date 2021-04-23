using System.Collections.Generic;

namespace MicrowaveNetworks.Touchstone
{
    /// <summary>Represents valid keywords defined in the Touchstone version 2.0 specifiation.</summary>
    /// <remarks>These keywords will not be present in a 1.0 version file, and some keywords are optional. Hence, most fields are nullable to
    /// indicate whether they are present in the file or should be included in the rendered file.</remarks>
    public class TouchstoneKeywords
    {
        /// <summary>Provides information on the Version of the specification under which the file contents should be interpreted.</summary>
        /// <remarks>This property will alwyas be set when a file is read for both 1.0 and 2.0 files so that the file version is clearly known. When a <see cref="TouchstoneFile"/> 
        /// object is created, the version and will be set by default to 1.0 in order to indicate what specification to generate the file in accordance with.</remarks>
        [TouchstoneKeyword("Version")]
        public FileVersion Version = FileVersion.One;
        /// <summary>Defines the number of single-ended ports represented by the network data in the file.</summary>
        /// <remarks>This value is ignored when saving data from a <see cref="TouchstoneFile"/> object since <see cref="TouchstoneFile.NetworkParameters"/>
        /// already defines the number of ports represented in the file.</remarks>
        [TouchstoneKeyword("Number of Ports")]
        public int NumberOfPorts;
        /// <summary>Signifies the column ordering convention for two-port network data.</summary>
        [TouchstoneKeyword("Two-Port Data Order")]
        public TwoPortDataOrderConfig? TwoPortDataOrder;
        /// <summary>Specifies the number of frequency points in the network data.</summary>
        [TouchstoneKeyword("Number of Frequencies")]
        public int? NumberOfFrequencies;
        /// <summary>Specifies the number of noise frequency points in the network data.</summary>
        [TouchstoneKeyword("Number of Noise Frequencies")]
        public int? NumberOfNoiseFrequencies;
        /// <summary>Provides a per-port definition of the reference environment used for the S-parameter measurements in the network data.</summary>
        [TouchstoneKeyword("Reference")]
        public List<float> Reference;
        /// <summary>Specifies whether an entire matrix or a subset of all matrix elements is given for single-ended data.</summary>
        [TouchstoneKeyword("Matrix Format")]
        public MatrixFormat? MatrixFormat;


    }

}
