using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using MicrowaveNetworks.Touchstone.IO;
using MicrowaveNetworks.Internal;
using System.Threading.Tasks;
using System.Text;

namespace MicrowaveNetworks.Touchstone
{



    /// <summary>
    /// Defines a complete Touchstone file according to the version 2.0 specification including the frequency dependent network data as well as the file options and keywords.
    /// </summary>
    /// <remarks>Use this class when writing to a Touchstone file for complete control of the final output, or when making in-memory modifications/round-trip edits to an existing file.
    /// If only the network data is needed from the file, you can use the <see cref="ReadNetworkData(string)"/> function to quickly access the data. Alternatively, you
    /// can use the low-level functions defined in <see cref="Touchstone.IO"/> for more complete control over file processing.
    /// <para></para>See the specification defined at http://ibis.org/touchstone_ver2.0/touchstone_ver2_0.pdf for more information.</remarks>
    public class TouchstoneFile
    {
        public TouchstoneOptions Options { get; set; } = new TouchstoneOptions();
        public TouchstoneKeywords Keywords { get; set; } = new TouchstoneKeywords();

        public NetworkParametersCollection NetworkParameters { get; set; }

        #region Constructors
        internal TouchstoneFile() { }

        /// <summary>Creates a new empty <see cref="TouchstoneFile"/> with a the specified ports and options.</summary>
        /// <param name="numPorts">The number of ports of the device that the Touchstone file will represent.</param>
        /// <param name="opts">The <see cref="TouchstoneOptions"/> that will define the format of the resulting file.</param>
        public TouchstoneFile(int numPorts, TouchstoneOptions opts)
        {
            Type parameterType = opts.Parameter.ToNetworkParameterMatrixType();
            NetworkParameters = new NetworkParametersCollection(numPorts, parameterType);
            Options = opts;
        }
        /// <summary>Creates a new Touchstone file from an existing <see cref="NetworkParametersCollection"/> with default settings.</summary>
        /// <param name="parameters">Specifies the network data that will comprise this Touchstone file.</param>
        public TouchstoneFile(NetworkParametersCollection parameters)
        {
            NetworkParameters = parameters;
        }
        /// <summary>
        /// Creates a new <see cref="TouchstoneFile"/> object by parsing the options, keywords, and network data contained within the specified file.
        /// </summary>
        /// <param name="filePath">The Touchstone (*.snp) file to be loaded.</param>
        /// <param name="settings">Additional settings to use to control how the file is loaded.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public TouchstoneFile(string filePath, TouchstoneReaderSettings settings)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            using (TouchstoneReader reader = TouchstoneReader.Create(filePath, settings))
            {
                FromFile(reader);
            }
        }
        #endregion
        #region IO



        /*public static async Task<Touchstone> FromFileAsync(string filePath, TouchstoneReaderSettings settings)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            using (TouchstoneReader reader = TouchstoneReader.CreateWithFile(filePath, settings))
            {
                TouchstoneParser parser = new TouchstoneParser(s);
                return await parser.ParseAsync();
            }
        }*/

        /// <summary>
        /// Reads the network data from the specified Touchstone file.
        /// </summary>
        /// <remarks>Use this method when you simply need access to the frequency dependent network parameters contained in the file and do not
        /// need to manipulate the file in memory or access to the <see cref="TouchstoneOptions"/> and <see cref="TouchstoneKeywords"/> in the file.</remarks>
        /// <param name="filePath">The Touchstone (*.snp) file to be loaded.</param>
        /// <returns>A new <see cref="NetworkParametersCollection"/> defining the frequency dependent network data.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public static NetworkParametersCollection ReadNetworkData(string filePath)
        {
            TouchstoneReaderSettings settings = new TouchstoneReaderSettings();
            return ReadNetworkData(filePath, settings);
        }
        /// <summary>
        /// Reads the network data from the specified Touchstone file.
        /// </summary>
        /// <remarks>Use this method when you simply need access to the frequency dependent network parameters contained in the file and do not
        /// need to manipulate the file in memory or access to the <see cref="TouchstoneOptions"/> and <see cref="TouchstoneKeywords"/> in the file.</remarks>
        /// <param name="filePath">The Touchstone (*.snp) file to be loaded.</param>
        /// <param name="settings">Additional settings to use to control how the file is loaded.</param>
        /// <returns>A new <see cref="NetworkParametersCollection"/> defining the frequency dependent network data.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public static NetworkParametersCollection ReadNetworkData(string filePath, TouchstoneReaderSettings settings)
        {

            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            using (TouchstoneReader reader = TouchstoneReader.Create(filePath, settings))
            {
                return ReadNetworkParameters(reader);
            }
        }
        /// <summary>Writes the Touchstone file object to the specified file with default writer settings.</summary>
        /// <param name="filePath">The *.sNp file to be created or overwritten.</param>
        /// <remarks>Use the <see cref="TouchstoneFileWriter"/> class for more control over the file writing process.</remarks>
        public void Write(string filePath)
        {
            Write(filePath, new TouchstoneWriterSettings());
        }
        /// <summary>Writes the Touchstone file object to the specified file with the specified writer settings.</summary>
        /// <param name="filePath">The *.sNp file to be created or overwritten.</param>
        /// <param name="settings">Additional settings regarding how the network data in the file should be written.</param>
        /// <remarks>Use the <see cref="TouchstoneFileWriter"/> class for more control over the file writing process.</remarks>
        public void Write(string filePath, TouchstoneWriterSettings settings)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            using (TouchstoneFileWriter writer = new TouchstoneFileWriter(filePath, settings))
            {
                writer.Options = Options;
                writer.Keywords = Keywords;

                foreach (var pair in NetworkParameters)
                {
                    writer.WriteEntry(pair);
                }

                writer.Flush();
            };
        }
        /// <summary>Asynchronously writes the Touchstone file object to the specified file with default writer settings.</summary>
        /// <param name="filePath">The *.sNp file to be created or overwritten.</param>
        /// <remarks>Use the <see cref="TouchstoneFileWriter"/> class for more control over the file writing process.</remarks>
        public async Task WriteAsync(string filePath, CancellationToken token = default)
        {
            await WriteAsync(filePath, new TouchstoneWriterSettings());
        }
        /// <summary>Asynchronously writes the Touchstone file object to the specified file with the specified writer settings.</summary>
        /// <param name="filePath">The *.sNp file to be created or overwritten.</param>
        /// /// <param name="settings">Additional settings regarding how the network data in the file should be written.</param>
        /// <remarks>Use the <see cref="TouchstoneFileWriter"/> class for more control over the file writing process.</remarks>
        public async Task WriteAsync(string filePath, TouchstoneWriterSettings settings, CancellationToken token = default)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            using (TouchstoneFileWriter writer = new TouchstoneFileWriter(filePath, settings))
            {
                writer.Options = Options;
                writer.Keywords = Keywords;
                writer.CancelToken = token;

                foreach (var pair in NetworkParameters)
                {
                    token.ThrowIfCancellationRequested();
                    await writer.WriteEntryAsync(pair);
                }

                writer.Flush();
            };
        }
        /*public static async Task<Touchstone> FromTextAsync(string fileText)
        {
            if (fileText == null) throw new ArgumentNullException(nameof(fileText));

            using (StringReader s = new StringReader(fileText))
            {
                TouchstoneParser parser = new TouchstoneParser(s);
                return await parser.ParseAsync();
            }
        }*/
        /*
        public static TouchstoneFile FromString(string fileText, TouchstoneReaderSettings settings)
        {
            if (fileText == null) throw new ArgumentNullException(nameof(fileText));

            using (TouchstoneReader reader = TouchstoneReader.CreateWithString(fileText, settings))
            {
                return new TouchstoneFile(reader);
            }
        }*/
        /// <summary>
        /// Renders the object as a properly formatted Touchstone file based on the configured Touchstone options.
        /// </summary>
        /// <returns>A string representation of a Touchstone file.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            using (TouchstoneWriter writer = TouchstoneWriter.Create(sb))
            {
                foreach (var data in NetworkParameters)
                {
                    writer.WriteData(data);
                }
                return sb.ToString();
            }
        }
        #endregion


        #region Internal
        private void FromFile(TouchstoneReader reader, CancellationToken token = default)
        {
            Options = reader.Options;
            Keywords = reader.Keywords;

            NetworkParameters = ReadNetworkParameters(reader, token);

            // If no version is set that makes this version 1.0
            Keywords.Version = Keywords.Version ?? FileVersion.One;
        }
        private static NetworkParametersCollection ReadNetworkParameters(TouchstoneReader reader, CancellationToken token = default)
        {
            NetworkParametersCollection networkParameters = null;
            foreach (var (frequency, matrix) in reader)
            {
                if (networkParameters == null)
                {
                    Type paramType = reader.Options.Parameter.ToNetworkParameterMatrixType();
                    networkParameters = new NetworkParametersCollection(matrix.NumPorts, paramType);
                }
                networkParameters.Add(frequency, matrix);
                token.ThrowIfCancellationRequested();
            }
            return networkParameters;
        }
        #endregion
    }
}
