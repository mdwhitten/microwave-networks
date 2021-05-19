using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using MicrowaveNetworks.Touchstone.IO;
using MicrowaveNetworks.Internal;
using System.Threading.Tasks;
using System.Text;
using MicrowaveNetworks.Matrices;

namespace MicrowaveNetworks.Touchstone
{



    /// <summary>
    /// Defines a complete Touchstone file according to the version 2.0 specification including the frequency dependent network data as well as the file options and keywords.
    /// </summary>
    /// <remarks>Use this class when writing to a Touchstone file for complete control of the final output, or when making in-memory modifications/round-trip edits to an existing file.
    /// If only the network data is needed from the file, you can use the <see cref="ReadAllData(string)"/> function to quickly access the data. Alternatively, you
    /// can use the low-level functions defined in <see cref="Touchstone.IO"/> for more complete control over file processing.
    /// <para></para>See the specification defined at http://ibis.org/touchstone_ver2.0/touchstone_ver2_0.pdf for more information.</remarks>
    public class TouchstoneFile
    {
        /// <summary>
        /// Gets or sets the <see cref="TouchstoneOptions"/> present in the Touchstone file.
        /// </summary>
        public TouchstoneOptions Options { get; set; } = new TouchstoneOptions();
        /// <summary>
        /// Gets or sets the <see cref="TouchstoneKeywords"/> present in the Touchstone file.
        /// </summary>
        /// <remarks>Keywords are only valid when <see cref="TouchstoneKeywords.Version"/> is 2.0.</remarks>
        public TouchstoneKeywords Keywords { get; set; } = new TouchstoneKeywords();

        /// <summary>
        /// Gets or sets the <see cref="INetworkParametersCollection"/> representing the network data present in the Touchstone file.
        /// </summary>
        public INetworkParametersCollection NetworkParameters { get; set; }

        #region Constructors
        internal TouchstoneFile() { }

        /// <summary>Creates a new empty <see cref="TouchstoneFile"/> with a the specified ports and options.</summary>
        /// <param name="numPorts">The number of ports of the device that the Touchstone file will represent.</param>
        /// <param name="opts">The <see cref="TouchstoneOptions"/> that will define the format of the resulting file.</param>
        public TouchstoneFile(int numPorts, TouchstoneOptions opts)
        {
            NetworkParameters = opts.Parameter switch
            {
                ParameterType.Scattering => new NetworkParametersCollection<ScatteringParametersMatrix>(numPorts),
                _ => throw new NotImplementedException()
            };
            Options = opts;
        }
        /// <summary>Creates a new Touchstone file from an existing <see cref="INetworkParametersCollection"/> with default settings.</summary>
        /// <param name="parameters">Specifies the network data that will comprise this Touchstone file.</param>
        public TouchstoneFile(INetworkParametersCollection parameters)
        {
            NetworkParameters = parameters;
            Keywords.NumberOfPorts = parameters.NumberOfPorts;
        }
        /// <summary>
        /// Creates a new <see cref="TouchstoneFile"/> object by parsing the options, keywords, and network data contained within the specified file.
        /// </summary>
        /// <param name="filePath">The Touchstone (*.snp) file to be loaded.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public TouchstoneFile(string filePath)
        {
            using TouchstoneReader reader = TouchstoneReader.Create(filePath);
            Options = reader.Options;
            Keywords = reader.Keywords;

            NetworkParameters = reader.ReadToEnd();
        }
        #endregion
        #region IO


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
        /// <remarks>Use the <see cref="TouchstoneWriter"/> class for more control over the file writing process.</remarks>
        public async Task WriteAsync(string filePath, CancellationToken token = default)
        {
            await WriteAsync(filePath, new TouchstoneWriterSettings());
        }
        /// <summary>Asynchronously writes the Touchstone file object to the specified file with the specified writer settings.</summary>
        /// <param name="filePath">The *.sNp file to be created or overwritten.</param>
        /// /// <param name="settings">Additional settings regarding how the network data in the file should be written.</param>
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
            foreach (var data in NetworkParameters)
            {
                writer.WriteData(data);
            }
            return sb.ToString();
        }
        #endregion

        #region Static Functions
        /// <summary>
        /// Enumerates the frequency-dependent network parameter data from the specified file.
        /// </summary>
        /// <param name="filePath">The Touchstone file to load.</param>
        /// <returns>All the network data loaded from the file.</returns>
        /// <remarks>Unlike <see cref="ReadAllData(string)"/>, this method returns an enumerable sequence of <see cref="FrequencyParametersPair"/> objects which are 
        /// loaded into memory one at a time. This is useful when using LINQ queries (such as limiting the number of frequencies to load) or passing the sequence
        /// to a collection to initialize. This avoids creating the whole collection in memory.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="filePath"/> is not found.</exception>
        /// <exception cref="InvalidDataException">Invalid data or format in <paramref name="filePath"/>.</exception>
        public static IEnumerable<FrequencyParametersPair> ReadData(string filePath)
        {
            using TouchstoneReader tsReader = TouchstoneReader.Create(filePath);
            while (tsReader.Read() is FrequencyParametersPair pair)
            {
                yield return pair;
            }
        }
        /// <summary>
        /// Enumerates the frequency-dependent network parameter data from the specified <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> to read network data from.</param>
        /// <returns>All the network data loaded from the reader.</returns>
        /// <remarks>Unlike <see cref="ReadAllData(TextReader)"/>, this method returns an enumerable sequence of <see cref="FrequencyParametersPair"/> objects which are 
        /// loaded into memory one at a time. This is useful when using LINQ queries (such as limiting the number of frequencies to load) or passing the sequence
        /// to a collection to initialize. This avoids creating the whole collection in memory.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="InvalidDataException">Invalid data or format in <paramref name="reader"/>.</exception>
        public static IEnumerable<FrequencyParametersPair> ReadData(TextReader reader)
        {
            using TouchstoneReader tsReader = TouchstoneReader.Create(reader);
            while (tsReader.Read() is FrequencyParametersPair pair)
            {
                yield return pair;
            }
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
        public static INetworkParametersCollection ReadAllData(string filePath)
        {
            using TouchstoneReader tsReader = TouchstoneReader.Create(filePath);
            return tsReader.ReadToEnd();
        }
        /// <summary>
        /// Reads all frequency-dependent network parameter data from the specified file. A <see cref="InvalidCastException"/> will be
        /// thrown if the Touchstone options line indicates a different type of data in the file than the requested type.
        /// </summary>
        /// <param name="filePath">The Touchstone file to read data from.</param>
        /// <returns>All the network data loaded from the file.</returns>
        /// <remarks>Unlike <see cref="ReadData(string)"/>, this method returns a <see cref="NetworkParametersCollection{TMatrix}"/> with all of the
        /// network data loaded into memory.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="filePath"/> is not found.</exception>
        /// <exception cref="InvalidDataException">Invalid data or format in <paramref name="filePath"/>.</exception>
        public static NetworkParametersCollection<T> ReadAllData<T>(string filePath) where T : NetworkParametersMatrix
        {
            using TouchstoneReader tsReader = TouchstoneReader.Create(filePath);
            Type fileParamType = tsReader.Options.Parameter.ToNetworkParameterMatrixType();
            if (fileParamType != typeof(T))
            {
                throw new InvalidCastException($"The specified Touchstone file contains parameter data of type {fileParamType.Name} which does not match the expected type " +
                    $"{typeof(T).Name}");
            }
            else
            {
                return (NetworkParametersCollection<T>)tsReader.ReadToEnd();
            }
        }
        /// <summary>
        /// Reads all frequency-dependent network parameter data from the specified <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> to read network data from.</param>
        /// <remarks>Unlike <see cref="ReadData(TextReader)"/>, this method returns a <see cref="INetworkParametersCollection"/> with all of the
        /// network data loaded into memory.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="InvalidDataException">Invalid data or format in <paramref name="reader"/>.</exception>
        public static INetworkParametersCollection ReadAllData(TextReader reader)
        {
            using TouchstoneReader tsReader = TouchstoneReader.Create(reader);
            return tsReader.ReadToEnd();
        }
        /// <summary>
        /// Reads all frequency-dependent network parameter data of type <typeparamref name="T"/> from the specified <see cref="TextReader"/>. A <see cref="InvalidCastException"/> will be
        /// thrown if the Touchstone options line indicates a different type of data in the file than the requested type.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> to read network data from.</param>
        /// <remarks>Unlike <see cref="ReadData(TextReader)"/>, this method returns a <see cref="NetworkParametersCollection{TMatrix}"/> with all of the
        /// network data loaded into memory.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="InvalidDataException">Invalid data or format in <paramref name="reader"/>.</exception>
        /// <exception cref="InvalidCastException">Data in file is not <typeparamref name="T"/></exception>
        public static NetworkParametersCollection<T> ReadAllData<T>(TextReader reader) where T : NetworkParametersMatrix
        {
            using TouchstoneReader tsReader = TouchstoneReader.Create(reader);
            Type fileParamType = tsReader.Options.Parameter.ToNetworkParameterMatrixType();
            if (fileParamType != typeof(T))
            {
                throw new InvalidCastException($"The specified Touchstone file contains parameter data of type {fileParamType.Name} which does not match the expected type " +
                    $"{typeof(T).Name}");
            }
            else
            {
                return (NetworkParametersCollection<T>)tsReader.ReadToEnd();
            }
        }
        #endregion
    }
}
