using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using MicrowaveNetworks.Touchstone.IO;
using MicrowaveNetworks.Internal;
using System.Threading.Tasks;
using System.Text;
using MicrowaveNetworks.Matrices;
using MicrowaveNetworks.Touchstone.Internal;

namespace MicrowaveNetworks.Touchstone
{



    /// <summary>
    /// Defines a complete Touchstone file according to the version 2.0 specification including the frequency dependent network data as well as the file options and keywords.
    /// </summary>
    /// <remarks>Use this class when writing to a Touchstone file for complete control of the final output, or when making in-memory modifications/round-trip edits to an existing file.
    /// If only the network data is needed from the file, you can use the <see cref="ReadAllData(string)"/> function to quickly access the data. Alternatively, you
    /// can use the low-level functions defined in <see cref="MicrowaveNetworks.Touchstone.IO"/> for more complete control over file processing.
    /// <para></para>See the specification defined at http://ibis.org/touchstone_ver2.0/touchstone_ver2_0.pdf for more information.</remarks>
    public class Touchstone
    {
        /// <summary>
        /// Gets the <see cref="INetworkParametersCollection"/> representing the network data present in the Touchstone file.
        /// </summary>
        public INetworkParametersCollection NetworkParameters { get; }

        /// <summary>Specifies the reference resistance in ohms, where <see cref="Resistance"/> is a real, positive number of ohms.
        /// The default value is set to 50 ohms.</summary>
        [TouchstoneParameter("R")]
        public float Resistance { get; set; } = 50;

        /// <summary>Provides a per-port definition of the reference environment used for the S-parameter measurements in the network data.</summary>
        [TouchstoneKeyword("Reference")]
        public List<float> Reference { get; set; }

        /// <summary>
        /// Gets the noise parameter data associated with the Touchstone file.
        /// </summary>
        [TouchstoneKeyword("NoiseData")]
        public Dictionary<double, TouchstoneNoiseData> NoiseData { get; set; }

        /// <summary>Contains additional metadata saved in the [Begin/End Information] section of the Touchstone file.</summary>
        public Dictionary<string, string> AdditionalInformation { get; set; }

        public Touchstone(INetworkParametersCollection networkParameters)
        {
            NetworkParameters = networkParameters;
        }
        public Touchstone(INetworkParametersCollection networkParameters, float resistance)
        {
            NetworkParameters = networkParameters;
            Resistance = resistance;
        }


        /// <summary>Writes the Touchstone file object to the specified file with default writer settings.</summary>
        /// <param name="filePath">The *.sNp file to be created or overwritten.</param>
        /// <remarks>Use the <see cref="TouchstoneWriter"/> class for more control over the file writing process.</remarks>
        public void Write(string filePath)
        {
            Write(filePath, new TouchstoneWriterSettings());
        }
        /// <summary>Writes the Touchstone file object to the specified file with the specified writer settings.</summary>
        /// <param name="filePath">The *.sNp file to be created or overwritten.</param>
        /// <param name="settings">Additional settings regarding how the network data in the file should be written.</param>
        /// <remarks>Use the <see cref="TouchstoneWriter"/> class for more control over the file writing process.</remarks>
        public void Write(string filePath, TouchstoneWriterSettings settings)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            using (TouchstoneWriter writer = TouchstoneWriter.Create(filePath, settings))
            {
                writer.Options = Options;
                writer.Keywords = Keywords;

                foreach (var pair in NetworkParameters)
                {
                    writer.WriteData(pair);
                }
                writer.Flush();
            };
        }
        /// <summary>Asynchronously writes the Touchstone file object to the specified file with default writer settings.</summary>
        /// <param name="filePath">The *.sNp file to be created or overwritten.</param>
        /// /// <param name="token">A <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <remarks>Use the <see cref="TouchstoneWriter"/> class for more control over the file writing process.</remarks>
        public async Task WriteAsync(string filePath, CancellationToken token = default)
        {
            await WriteAsync(filePath, new TouchstoneWriterSettings(), token);
        }
        /// <summary>Asynchronously writes the Touchstone file object to the specified file with the specified writer settings.</summary>
        /// <param name="filePath">The *.sNp file to be created or overwritten.</param>
        /// <param name="settings">Additional settings regarding how the network data in the file should be written.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <remarks>Use the <see cref="TouchstoneWriter"/> class for more control over the file writing process.</remarks>
        public async Task WriteAsync(string filePath, TouchstoneWriterSettings settings, CancellationToken token = default)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            using (TouchstoneWriter writer = TouchstoneWriter.Create(filePath, settings))
            {
                writer.Options = Options;
                writer.Keywords = Keywords;
                writer.CancelToken = token;

                foreach (var pair in NetworkParameters)
                {
                    token.ThrowIfCancellationRequested();
                    await writer.WriteDataAsync(pair);
                }

                writer.Flush();
            };
        }

        /// <summary>
        /// Renders the object as a properly formatted Touchstone file based on the configured Touchstone options.
        /// </summary>
        /// <returns>A string representation of a Touchstone file.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            using TouchstoneWriter writer = TouchstoneWriter.Create(sb);
            writer.Options = Options;
            writer.Keywords = Keywords;

            foreach (var data in NetworkParameters)
            {
                writer.WriteData(data);
            }
            return sb.ToString();
        }


        /// <summary>
        /// Reads all frequency-dependent network parameter data from the specified file.
        /// </summary>
        /// <param name="filePath">The Touchstone file to read data from.</param>
        /// <returns>All the network data loaded from the file.</returns>
        /// <remarks>Unlike <see cref="ReadData(string)"/>, this method returns a <see cref="INetworkParametersCollection"/> with all of the
        /// network data loaded into memory.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="filePath"/> is not found.</exception>
        /// <exception cref="InvalidDataException">Invalid data or format in <paramref name="filePath"/>.</exception>
        {
            using TouchstoneReader tsReader = TouchstoneReader.Create(filePath);
        }
    }
}
