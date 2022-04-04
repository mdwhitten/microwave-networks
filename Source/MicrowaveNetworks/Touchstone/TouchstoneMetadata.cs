using MicrowaveNetworks.Touchstone.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicrowaveNetworks.Touchstone
{
    /// <summary>
    /// Defines additional metadata found in the Touchstone file.
    /// </summary>
    /// <remarks>The value <see cref="Resistance"/> will exists in either <see cref="TouchstoneFileVersion.One"/> or 
    /// <see cref="TouchstoneFileVersion.Two"/> file formats. All other metadata only exists in <see cref="TouchstoneFileVersion.Two"/>
    /// file formats. Collections are initialized but empty for <see cref="TouchstoneFileVersion.One"/> files or if the data
    /// is not present. When empty, the values will also be ignored when writing the file to disk..</remarks>
    public class TouchstoneMetadata
    {
        internal TouchstoneMetadata(TouchstoneOptions options, TouchstoneKeywords keywords)
        {
            Resistance = options.Resistance;

            if (keywords.Version == TouchstoneFileVersion.Two)
            {
                Reference = keywords.Reference;

            }
        }

        public TouchstoneMetadata() { }

        /// <summary>Specifies the reference resistance in ohms, where <see cref="Resistance"/> is a real, positive number of ohms.
        /// The default value is set to 50 ohms.</summary>
        [TouchstoneParameter("R")]
        public float Resistance { get; set; } = 50;

        /// <summary>Provides a per-port definition of the reference environment used for the S-parameter measurements in the network data.</summary>
        [TouchstoneKeyword("Reference")]
        public List<float> Reference { get; set; } = new List<float>();

        /// <summary>Contains additional metadata saved in the [Begin/End Information] section of the Touchstone file.</summary>
        public Dictionary<string, string> AdditionalInformation { get; set; } = new Dictionary<string, string>();

        internal ParameterType ParameterType { get; set; } = ParameterType.Scattering;
    }
}
