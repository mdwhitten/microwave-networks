using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using MicrowaveNetworks.Matrices;
using MicrowaveNetworks.Touchstone.Internal;
using MicrowaveNetworks.Internal;

#nullable enable

namespace MicrowaveNetworks.Touchstone.IO
{
    /// <summary>
    /// Provides lower-level support for reading Touchstone files from existing data sources.
    /// </summary>
    public sealed partial class TouchstoneReader : IDisposable

    {
        public List<string> Comments { get; } = new List<string>();
        /// <summary>
        /// Specifies the reference resistance in ohms, where <see cref="Resistance"/> is a real, positive number of ohms.
        /// If the <see cref="TouchstoneParameterAttribute"/> "R" is complex it will be represented by its real part. 
        /// </summary>
        public float Resistance { get; private set; }
        public float? Reactance { get; private set; }


        /// <summary>Provides a per-port definition of the reference environment used for the S-parameter measurements in the network data.</summary>
        [TouchstoneKeyword("Reference")]
        public List<float>? Reference { get; private set; }

        /// <summary>
        /// Gets the noise parameter data associated with the Touchstone file.
        /// </summary>
        [TouchstoneKeyword("NoiseData")]
        public Dictionary<double, TouchstoneNoiseData>? NoiseData { get; private set; }

        /// <summary>Contains additional metadata saved in the [Begin/End Information] section of the Touchstone file.</summary>
        public string? AdditionalInformation { get; private set; }


        /// <summary>Gets the <see cref="TouchstoneOptionsLine"/> parameters parsed from the options line in the Touchstone file.</summary>
        internal TouchstoneOptionsLine Options { get; }


        private static readonly FieldNameLookup<TouchstoneKeywords> keywordLookup = new FieldNameLookup<TouchstoneKeywords>();
        private static readonly string resistanceSignifier = GetTouchstoneFieldName<TouchstoneOptionsLine>(nameof(TouchstoneOptionsLine.Resistance));
        private static readonly string referenceKeywordName = GetTouchstoneFieldName<TouchstoneKeywords>(nameof(TouchstoneKeywords.Reference));


        private readonly TextReader reader;
        private readonly TouchstoneReaderSettings settings;
        private int lineNumber;
        private readonly TouchstoneReaderCore coreReader;


        private TouchstoneReader(TextReader reader, TouchstoneReaderSettings settings)
        {
            this.settings = settings ?? new TouchstoneReaderSettings();
            this.reader = reader;
            Options = new TouchstoneOptionsLine();

            coreReader = TouchstoneReaderCore.Create(this);
        }

        #region Constructors
        /// <summary>
        /// Creates a new <see cref="TouchstoneReader"/> by opening the Touchstone file specified in <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">The Touchstone (*.sNp) file to read network data from.</param>
        /// <returns>An object used to read network data from the Touchstone file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="InvalidDataException">Invalid data format in the Touchstone file</exception>
        public static TouchstoneReader Create(string filePath) => Create(filePath, new TouchstoneReaderSettings());
        // Private for now since settings don't do anything
        private static TouchstoneReader Create(string filePath, TouchstoneReaderSettings settings)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            StreamReader reader = new StreamReader(filePath);
            try
            {
                return new TouchstoneReader(reader, settings);
            }
            catch
            {
                reader.Dispose();
                throw;
            }
        }
        /// <summary>
        /// Creates a new <see cref="TouchstoneReader"/> with the specified text reader.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> from which to read the network data.</param>
        /// <returns>An object used to read network data from the Touchstone file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="InvalidDataException">Invalid data format in the Touchstone file</exception>
        public static TouchstoneReader Create(TextReader reader) => Create(reader, new TouchstoneReaderSettings());
        private static TouchstoneReader Create(TextReader reader, TouchstoneReaderSettings settings)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            return new TouchstoneReader(reader, settings);
        }
        #endregion
        /// <summary>
        /// Reads the next <see cref="FrequencyParametersPair"/> from the Touchstone data from the input source.
        /// </summary>
        /// <returns>A new <see cref="FrequencyParametersPair"/> object with a <see cref="NetworkParametersMatrix"/> and associated frequency, or null if no more data
        /// is available.</returns>
        /// <exception cref="InvalidDataException">Data or format in file is bad.</exception>
        /// <exception cref="ObjectDisposedException">Reader has been disposed.</exception>
        public FrequencyParametersPair? Read()
        {
            if (!disposedValue)
            {
                return coreReader.ReadNextMatrix();
            }
            else throw new ObjectDisposedException(nameof(TouchstoneReader));
        }
        /// <summary>
        /// Reads all remaining available network data from the Touchstone file from the current point.
        /// </summary>
        /// <returns>A <see cref="INetworkParametersCollection"/> containing each pair of frequency and <see cref="NetworkParametersMatrix"/> objects,
        /// or null if no more data is available.</returns>
        /// <exception cref="InvalidDataException">Data or format in file is bad.</exception>
        /// <exception cref="ObjectDisposedException">Reader has been disposed.</exception>
        public INetworkParametersCollection ReadToEnd()
        {
            if (!disposedValue)
            {
                INetworkParametersCollection collection = null;

                // Read returns a nullable FrequencyParametersPair object; in this statement,
                // we use is to validate that it isn't null and break into its parts in a single step.
                while (Read() is (double frequency, NetworkParametersMatrix matrix))
                {
                    if (collection == null)
                    {
                        int numPorts = matrix.NumPorts;
                        collection = Options.Parameter switch
                        {
                            ParameterType.Scattering => new NetworkParametersCollection<ScatteringParametersMatrix>(numPorts),
                            _ => throw new NotImplementedException($"Support for parameter type {Options.Parameter} has not been implemented."),
                        };
                    }
                    collection[frequency] = matrix;
                }
                return collection;
            }
            else throw new ObjectDisposedException(nameof(TouchstoneReader));
        }
        #region Parsing
        private (TouchstoneKeywords Keyword, string Value) ParseKeyword(string line)
        {
            var match = Regex.Match(line, @"[(\w+)]\s(\w+)?");

            if (!match.Success) ThrowHelper("Keywords", "Bad keyword format");

            string keywordName = match.Groups[1].Value;
            string value = match.Groups[2].Value;

            if (!TouchstoneEnumMap<TouchstoneKeywords>.TryFromTouchstoneValue(keywordName, out TouchstoneKeywords keyword))
            {
                ThrowHelper("Keywords", "Unknown keyword");
            }
            return (keyword, value);
        }
        private void ParseOption(string line)
        {
            string[] options = Regex.Split(line, @"\s+");

            // Skip the first element since it will still contain the "#"
            IEnumerable<string> optionsEnumerable = options.Skip(1);

            // We will manually control the enumerator here since the last item (resistance)
            // has to fetch the next item in sequence
            using var enumer = optionsEnumerable.GetEnumerator();

            while (enumer.MoveNext())
            {
                string option = enumer.Current;
                // Format specifies that options can occur in any order
                if (TouchstoneEnumMap<TouchstoneFrequencyUnit>.ValidTouchstoneName(option))
                {
                    //string frequencyUnitName = frequencyUnitLookup.Value[option];
                    Options.FrequencyUnit = TouchstoneEnumMap<TouchstoneFrequencyUnit>.FromTouchstoneValue(option);
                }
                else if (TouchstoneEnumMap<TouchstoneDataFormat>.ValidTouchstoneName(option))
                {
                    Options.Format = TouchstoneEnumMap<TouchstoneDataFormat>.FromTouchstoneValue(option);
                }
                else if (TouchstoneEnumMap<ParameterType>.ValidTouchstoneName(option))
                {
                    Options.Parameter = TouchstoneEnumMap<ParameterType>.FromTouchstoneValue(option);
                }
                else if (option.Equals(resistanceSignifier, StringComparison.OrdinalIgnoreCase))
                {
                    // For resistance, this option is specified in the format of "R [value]"
                    // Hence, we need to actually move the enumerator forward to get the value
                    bool success = enumer.MoveNext();
                    if (success)
                    {
                        string value = enumer.Current;

                        bool parsed = TryParseImpedance(value, out float r, out float x);

                        if (parsed)
                        {
                            Options.Resistance = r;
                            Options.Reactance = x;
                        }
                        else ThrowHelper("Options", "Bad value for resistance");
                    }
                    else ThrowHelper("Options", "No value specified for resistance");
                }
                else
                {
                    ThrowHelper("Options", $"Invalid option value {option}");
                }
            }
        }
        private (double Frequency, List<NetworkParameter> Parameters) ParseRawData(List<string> rawFlattenedMatrix)
        {
            string frequencyString = rawFlattenedMatrix[0];
            double frequency = ParseFrequency(frequencyString);
            rawFlattenedMatrix.RemoveAt(0);
            List<NetworkParameter> parameters = ParseParameters(rawFlattenedMatrix);

            return (frequency, parameters);
        }
        private List<NetworkParameter> ParseParameters(List<string> data)
        {
            List<NetworkParameter> parameters = new List<NetworkParameter>();


            for (int j = 0; j < data.Count; j += 2)
            {
                double val1 = 0, val2 = 0;
                try
                {
                    val1 = double.Parse(data[j]);
                    val2 = double.Parse(data[j + 1]);
                }
                catch (FormatException)
                {
                    ThrowHelper("Data", "Invalid data format");
                }
                NetworkParameter param = new NetworkParameter();
                switch (Options.Format)
                {
                    case TouchstoneDataFormat.DecibelAngle:
                        param = NetworkParameter.FromPolarDecibelDegree(val1, val2);
                        break;
                    case TouchstoneDataFormat.MagnitudeAngle:
                        param = NetworkParameter.FromPolarDegree(val1, val2);
                        break;
                    case TouchstoneDataFormat.RealImaginary:
                        param = new NetworkParameter(val1, val2);
                        break;
                }
                parameters.Add(param);
            }
            return parameters;
        }
        private double ParseFrequency(string frequencyString)
        {
            bool success = double.TryParse(frequencyString, out double frequency);
            if (!success) ThrowHelper("Data", "Invalid format for frequency");

            frequency *= Options.FrequencyUnit.GetMultiplier();

            return frequency;
        }
        private static List<string> TrimAndSplitLine(string line)
        {
            // Remove any trailing comments and any leading or trailing whitespace
            line = StripTrailingComment(line).Trim();

            string[] data = Regex.Split(line, @"\s+");
            return new List<string>(data);
        }
        private bool TryParseImpedance(string impedance, out float r, out float x)
        {
            r = 0;
            x = 0;
            bool parsed = float.TryParse(impedance, out r);
            if (!parsed)
            {
                Match m = Regex.Match(impedance, @"\((?<r>\d+)(?<sign>[+-])(?<x>\d+)j\)");
                if (m.Success)
                {
                    r = float.Parse(m.Groups["r"].Value);
                    x = float.Parse(m.Groups["x"].Value);
                    int sign = m.Groups["sign"].Value.Contains("-") ? -1 : 1;
                    x *= sign;
                }
                return m.Success;
            }
            return parsed;

        }
        #endregion
        #region TextReader Helper Functions
        private bool MoveToNextValidLine()
        {
            int nextCharInt;

            while ((nextCharInt = reader.Peek()) != -1)
            {
                char nextChar = (char)nextCharInt;
                switch (nextChar)
                {
                    // If it's a space, advance forward by character until we hit a definitive value
                    case ' ':
                        reader.Read();
                        break;
                    // For new lines and comments, skip to the next line
                    case Constants.CommentChar:
                        Comments.Add(reader.ReadLine());
                        lineNumber++;
                        break;
                    case var _ when char.IsWhiteSpace(nextChar):
                        reader.ReadLine();
                        lineNumber++;
                        break;
                    // Anything else, return to the caller
                    default:
                        return true;
                }
            }
            return false;
        }
        private async Task<bool> MoveToNextValidLineAsync()
        {
            char nextChar;

            while ((nextChar = (char)reader.Peek()) != -1)
            {
                switch (nextChar)
                {
                    // If it's a space, advance forward by character until we hit a definitive value
                    case ' ':
                        reader.Read();
                        break;
                    // For new lines and comments, skip to the next line
                    case Constants.CommentChar:
                        Comments.Add(await reader.ReadLineAsync());
                        lineNumber++;
                        break;
                    case var _ when char.IsWhiteSpace(nextChar):
                        await reader.ReadLineAsync();
                        lineNumber++;
                        break;
                    // Anything else, return to the caller
                    default:
                        return true;
                }
            }
            return false;
        }
        private string ReadLineAndCount()
        {
            lineNumber++;
            return reader.ReadLine();
        }
        private async Task<string> ReadLineAndCountAsync()
        {
            lineNumber++;
            return await reader.ReadLineAsync();
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
        #region Utilities
        private static string GetTouchstoneFieldName<T>(string objectFieldName)
        {
            FieldInfo f = typeof(T).GetField(objectFieldName);
            var attr = f.GetCustomAttribute<TouchstoneParameterAttribute>();
            return attr.FieldName;
        }
        private static string StripTrailingComment(string line)
        {
            int index = line.IndexOf(Constants.CommentChar);
            if (index >= 0)
            {
                return line.Substring(0, index);
            }
            else return line;
        }
        private void ThrowHelper(string sectionName, string extraMessage = null, Exception inner = null)
        {
            string message = $"Invalid data format parsing section {sectionName} at line {lineNumber}.";
            if (!string.IsNullOrEmpty(extraMessage)) message += $" Parser returned message \"{extraMessage}\".";
            if (inner != null)
            {
                throw new InvalidDataException(message);
            }
            else throw new InvalidDataException(message, inner);
        }

        #endregion
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    reader?.Dispose();
                }
                disposedValue = true;
            }
        }
        /// <summary>
        /// Disposes the underlying <see cref="TextReader"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
