using System;
using System.Collections.Generic;
using System.Text;
using MicrowaveNetworks.Touchstone.Internal;
using MicrowaveNetworks.Touchstone.IO;

namespace MicrowaveNetworks.Touchstone
{
    /// <summary>
    /// Defines a complete Touchstone file according to the version 2.0 specification including the frequency dependent network data as well as the file options and keywords.
    /// </summary>
    /// <remarks>Use this class when writing to a Touchstone file for complete control of the final output, or when making in-memory modifications/round-trip edits to an existing file.
    /// If only the network data is needed from the file, you can use the <see cref="ReadAllData(string)"/> function to quickly access the data. Alternatively, you
    /// can use the low-level functions defined in <see cref="MicrowaveNetworks.Touchstone.IO"/> for more complete control over file processing.
    public abstract class TouchstoneBase
    {
        internal TouchstoneBase(INetworkParametersCollection networkParameters)
        {
            NetworkParameters = networkParameters;
        }
        internal TouchstoneBase(INetworkParametersCollection networkParameters, float resistance)
        {
            NetworkParameters = networkParameters;
            Resistance = resistance;
        }


        /// <summary>
        /// Gets the <see cref="INetworkParametersCollection"/> representing the network data present in the Touchstone file.
        /// </summary>
        public INetworkParametersCollection NetworkParameters { get; }

        /// <summary>Specifies the reference resistance in ohms, where <see cref="Resistance"/> is a real, positive number of ohms.
        /// The default value is set to 50 ohms.</summary>
        [TouchstoneParameter("R")]
        public float Resistance { get; set; } = 50;




    }
}
