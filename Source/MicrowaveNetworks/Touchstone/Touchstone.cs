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

#nullable enable

namespace MicrowaveNetworks.Touchstone
{
    /// <summary>
    /// Defines a complete Touchstone file according to the version 2.0 specification including the frequency dependent network data.
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
		/// <summary>
		/// Specifies the reference reactance in ohms, where <see cref="Reactance"/> is a real number of ohms. 
		/// If the <see cref="TouchstoneParameterAttribute"/> "R" is complex it will be represented by its imaginary part.
		/// Otherwise it is considered to be 0.
		/// </summary>
		/// <remarks>This parameter is not specified by Touchstone standard while scikit-rf has an implementation for it.</remarks>
		[TouchstoneParameter("R")]
		public float? Reactance { get; set; }

		/// <summary>Provides a per-port definition of the reference environment used for the S-parameter measurements in the network data.</summary>
		[TouchstoneKeyword("Reference")]
        public List<float>? Reference { get; }

        /// <summary>
        /// Gets the noise parameter data associated with the Touchstone file.
        /// </summary>
        [TouchstoneKeyword("NoiseData")]
        public Dictionary<double, TouchstoneNoiseData>? NoiseData { get; }

        /// <summary>Contains additional metadata saved in the [Begin/End Information] section of the Touchstone file.</summary>
        public string? AdditionalInformation { get; set; }

		/// <summary>
		/// Initializes a new Touchstone object by loading the data from the specified file path.
		/// </summary>
		/// <param name="filePath">The file path containing Touchstone data.</param>
		public Touchstone(string filePath)
        {
            using TouchstoneReader tsReader = TouchstoneReader.Create(filePath);

            NetworkParameters = tsReader.ReadToEnd();
            Resistance = tsReader.Resistance;
            Reactance = tsReader.Reactance;
            if (tsReader.Reference != null) Reference = new List<float>(tsReader.Reference);
            if (tsReader.NoiseData != null) NoiseData = tsReader.NoiseData;
            AdditionalInformation = tsReader.AdditionalInformation;
        }

        /// <summary>
        /// Initializes a new Touchstone object by parsing the data from the specified <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> to parse the Touchstone data from.</param>
		public Touchstone(TextReader reader)
		{
			using TouchstoneReader tsReader = TouchstoneReader.Create(reader);

			NetworkParameters = tsReader.ReadToEnd();
			Resistance = tsReader.Resistance;
			Reactance = tsReader.Reactance;
			if (tsReader.Reference != null) Reference = new List<float>(tsReader.Reference);
			if (tsReader.NoiseData != null) NoiseData = tsReader.NoiseData;
			AdditionalInformation = tsReader.AdditionalInformation;
		}

		/// <summary>
		/// Initializes a new <see cref="Touchstone"/> object with the specified number of ports with an empty <see cref="NetworkParametersCollection{TMatrix}"/> of type <see cref="ScatteringParametersMatrix"/> and a 
		/// resistance of 50 ohms.
		/// </summary>
		/// <param name="numberOfPorts">Specifies the number of ports for the <see cref="NetworkParameters"/> collection.</param>
		public Touchstone(int numberOfPorts)
        {
            NetworkParameters = new NetworkParametersCollection<ScatteringParametersMatrix>(numberOfPorts);

            Resistance = 50.0f;
        }

		/// <summary>
		/// Creates a new Toucshtone object with the specified number of ports and a resistance of 50 ohms, using the specified type of <see cref="NetworkParametersMatrix"/>.
		/// </summary>
		/// <typeparam name="TMatrixType">Specifies the network parameter type of the new Touchstone object.</typeparam>
		/// <param name="numberOfPorts">Specifies the number of ports for the <see cref="NetworkParameters"/> collection.</param>
		/// <param name="resistance">Specifies reference resistance in ohms.</param>
		/// <returns>A new Touchstone object.</returns>
		public static Touchstone Create<TMatrixType>(int numberOfPorts, float resistance = 50.0f) where TMatrixType : NetworkParametersMatrix
        {
            NetworkParametersCollection<TMatrixType> collection = new NetworkParametersCollection<TMatrixType>(numberOfPorts);

            return new Touchstone(collection, resistance);
        }
		/// <summary>Initializes a new Toucshtone object with the specified number network data and resistance value.</summary>
		/// <param name="networkParameters">Specifies the network data.</param>
		/// <param name="resistance">Specifies reference resistance in ohms.</param>
		public Touchstone(INetworkParametersCollection networkParameters, float resistance = 50)
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

            using (TouchstoneWriter writer = TouchstoneWriter.Create(filePath, this, settings))
            {
                writer.WriteNetworkData();
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

            using (TouchstoneWriter writer = TouchstoneWriter.Create(filePath, this, settings))
            {
                //writer.CancelToken = token;
                await writer.WriteNetworkDataAsync(token);
                await writer.FlushAsync();
            };
        }

        /// <summary>
        /// Renders the object as a properly formatted Touchstone file with default <see cref="TouchstoneWriterSettings"/>.
        /// </summary>
        /// <returns>A string representation of a Touchstone file.</returns>
        public override string ToString() => ToString(new TouchstoneWriterSettings());

		/// <summary>
		/// Renders the object as a properly formatted Touchstone file with the specified <see cref="TouchstoneWriterSettings"/>.
		/// </summary>
        /// <param name="settings">Specifies the settings used to format the Touchstone file.</param>
		/// <returns>A string representation of a Touchstone file.</returns>
		public string ToString(TouchstoneWriterSettings settings)
        {
			StringBuilder sb = new StringBuilder();

            using (TouchstoneWriter writer = TouchstoneWriter.Create(sb, this, settings))
            {
                writer.WriteNetworkData();
            }
			return sb.ToString();
		}

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
