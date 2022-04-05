using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using MicrowaveNetworks.Matrices;

namespace MicrowaveNetworks.Touchstone.IO
{
    /// <summary>
    /// Provides lower-level support for reading Touchstone files from existing data sources.
    /// </summary>
    public sealed class TouchstoneReader : IDisposable

    {
        /// <summary>Gets the keywords declared in the Touchstone file if the file version specification is 2.0.</summary>
        public TouchstoneKeywords Keywords { get; }
        /// <summary>Gets the <see cref="TouchstoneOptions"/> parameters parsed from the options line in the Touchstone file.</summary>
        public TouchstoneOptions Options { get; }

        private static readonly FieldNameLookup<TouchstoneKeywords> keywordLookup = new FieldNameLookup<TouchstoneKeywords>();
        private static readonly string resistanceSignifier = GetTouchstoneFieldName<TouchstoneOptions>(nameof(TouchstoneOptions.Resistance));
        private static readonly string referenceKeywordName = GetTouchstoneFieldName<TouchstoneKeywords>(nameof(TouchstoneKeywords.Reference));


        private readonly TextReader reader;
        private readonly TouchstoneReaderSettings settings;
        private int lineNumber;
        private readonly TouchstoneReaderCore coreReader;


        private TouchstoneReader(TextReader reader, TouchstoneReaderSettings settings)
        {
            this.settings = settings ?? new TouchstoneReaderSettings();
            this.reader = reader;
            Options = new TouchstoneOptions();
            Keywords = new TouchstoneKeywords();

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
                            _ => throw new NotImplementedException(),
                        };
                    }
                    collection[frequency] = matrix;
                }
                return collection;
            }
            else throw new ObjectDisposedException(nameof(TouchstoneReader));
        }
        #region Parsing
        private void ParseKeyword(string line)
        {
            var match = Regex.Match(line, @"[(\w+)]\s(\w+)?");

            if (!match.Success) ThrowHelper("Keywords", "Bad keyword format");

            // All keywords are of format [Keyword] Value except for the [Reference] keyword, whose data is on a second line after the 
            // keyword. If the group 2 match is successful, then this keyword follows the primary format.
            if (match.Groups[2].Success)
            {
                string keywordName = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                bool found = keywordLookup.Value.TryGetValue(keywordName, out string fieldName);
                if (!found) ThrowHelper("Keywords", "Unknown keyword");

                FieldInfo field = typeof(TouchstoneKeywords).GetField(fieldName);
                object convertedValue = null;
                try
                {
                    convertedValue = Convert.ChangeType(value, field.FieldType);
                }
                catch (Exception ex) when (ex is InvalidCastException || ex is FormatException)
                {
                    ThrowHelper("Keywords", "Bad keyword value", ex);
                }

                field.SetValue(Keywords, convertedValue);
            }
            // If the second group wasn't found above but this keyword is the [Reference] keyword, try loading the next line expecting the 
            // reference data.
            else if (match.Groups[1].Value == referenceKeywordName)
            {
                throw new NotImplementedException();
            }
            // Any other situation is an error.
            else ThrowHelper("Keywords", "Invalid keyword format");
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
                if (TouchstoneEnumMap<FrequencyUnit>.ValidTouchstoneName(option))
                {
                    //string frequencyUnitName = frequencyUnitLookup.Value[option];
                    Options.FrequencyUnit = TouchstoneEnumMap<FrequencyUnit>.FromTouchstoneValue(option);
                }
                else if (TouchstoneEnumMap<FormatType>.ValidTouchstoneName(option))
                {
                    Options.Format = TouchstoneEnumMap<FormatType>.FromTouchstoneValue(option);
                }
                else if (TouchstoneEnumMap<ParameterType>.ValidTouchstoneName(option))
                {
                    Options.Parameter = TouchstoneEnumMap<ParameterType>.FromTouchstoneValue(option);
                }
                else if (option == resistanceSignifier)
                {
                    // For resistance, this option is specified in the format of "R [value]"
                    // Hence, we need to actually move the enumerator forward to get the value
                    bool success = enumer.MoveNext();
                    if (success)
                    {
                        string value = enumer.Current;

                        bool parsed = float.TryParse(value, out float r);
                        if (parsed) Options.Resistance = r;
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
                    case FormatType.DecibelAngle:
                        param = NetworkParameter.FromPolarDecibelDegree(val1, val2);
                        break;
                    case FormatType.MagnitudeAngle:
                        param = NetworkParameter.FromPolarDegree(val1, val2);
                        break;
                    case FormatType.RealImaginary:
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

        #region Core Reader Classes
        abstract class TouchstoneReaderCore
        {
            protected TouchstoneReader tsReader;

            protected TouchstoneReaderCore(TouchstoneReader reader)
            {
                this.tsReader = reader;
            }
            protected abstract void ReadHeader(string currentLine);

            public abstract FrequencyParametersPair? ReadNextMatrix();
            //protected abstract Task<(bool eof, FrequencyParametersPair matrix)> ReadNextMatrixAsync();

            public static TouchstoneReaderCore Create(TouchstoneReader tsReader)
            {
                TouchstoneReaderCore readerCore = null;
                string firstLine = default;

                if (tsReader.MoveToNextValidLine())
                {
                    firstLine = tsReader.ReadLineAndCount();
                }
                else tsReader.ThrowHelper("Header", "No valid information contained in file.");

                firstLine = firstLine.Trim();
                if (firstLine[0] == Constants.OptionChar)
                {
                    readerCore = new TouchstoneReaderCoreV1(tsReader);
                }
                else if (firstLine[0] == Constants.KeywordOpenChar)
                {
                    readerCore = new TouchstoneReaderCoreV2(tsReader);
                }
                else
                {
                    tsReader.ThrowHelper("Header", "The Option Line (Touchstone format 1.0) or Version Keyword (Touchstone format 2.0) must be the first" +
                        "non-comment and non-blank line in the file.");
                }
                readerCore.ReadHeader(firstLine);
                return readerCore;
            }
        }
        class TouchstoneReaderCoreV1 : TouchstoneReaderCore
        {
            internal TouchstoneReaderCoreV1(TouchstoneReader reader) : base(reader) { }
            int? flattenedMatrixLength;
            readonly Queue<string> previewedLines = new Queue<string>();

            protected override void ReadHeader(string currentLine)
            {
                tsReader.ParseOption(currentLine);
            }
            public override FrequencyParametersPair? ReadNextMatrix()
            {
                List<string> rawFlattenedMatrix = new List<string>();
                FrequencyParametersPair? networkData = default;

                if (!flattenedMatrixLength.HasValue)
                {
                    if (!tsReader.MoveToNextValidLine())
                    {
                        tsReader.ThrowHelper("Data");
                    }
                    string firstLine = tsReader.ReadLineAndCount();
                    rawFlattenedMatrix.AddRange(TrimAndSplitLine(firstLine));

                    // We only need to perform this check if the network has 2 ports or more; a one port network only has a single
                    // data pair (i.e. two entries) plus frequency. We know that we don't need to investigate subsequent lines.
                    if (rawFlattenedMatrix.Count > 3)
                    {
                        while (tsReader.MoveToNextValidLine())
                        {
                            string line = tsReader.ReadLineAndCount();
                            var data = TrimAndSplitLine(line);
                            // Continued data lines split over multiple should always have an even number (pairs of complex data).
                            // New frequency points will have an odd number of values due to the frequency being present
                            if (data.Count % 2 == 0)
                            {
                                rawFlattenedMatrix.AddRange(data);
                            }
                            else
                            {
                                previewedLines.Enqueue(line);
                                break;
                            }
                        }
                    }
                    flattenedMatrixLength = rawFlattenedMatrix.Count;
                }
                else
                {
                    while (previewedLines.Count > 0 && rawFlattenedMatrix.Count < flattenedMatrixLength.Value)
                    {
                        string line = previewedLines.Dequeue();
                        rawFlattenedMatrix.AddRange(TrimAndSplitLine(line));
                    }
                    while (rawFlattenedMatrix.Count < flattenedMatrixLength.Value && tsReader.MoveToNextValidLine())
                    {
                        string line = tsReader.ReadLineAndCount();
                        rawFlattenedMatrix.AddRange(TrimAndSplitLine(line));
                    }
                }

                if (rawFlattenedMatrix.Count == flattenedMatrixLength.Value)
                {
                    var (frequency, parameters) = tsReader.ParseRawData(rawFlattenedMatrix);

                    NetworkParametersMatrix matrix = tsReader.Options.Parameter switch
                    {
                        ParameterType.Scattering => new ScatteringParametersMatrix(parameters, ListFormat.SourcePortMajor),
                        _ => throw new NotImplementedException($"Support for parameter type {tsReader.Options.Parameter} has not been implemented."),
                    };

                    networkData = new FrequencyParametersPair(frequency, matrix);
                }

                return networkData;
            }

            /*protected override Task<FrequencyParametersPair> ReadNextMatrixAsync()
            {
                throw new NotImplementedException();
            }*/
        }
        class TouchstoneReaderCoreV2 : TouchstoneReaderCore
        {
            internal TouchstoneReaderCoreV2(TouchstoneReader reader) : base(reader) { }

            protected override void ReadHeader(string currentLine)
            {
                throw new NotImplementedException();
            }

            public override FrequencyParametersPair? ReadNextMatrix()
            {
                /*if (selected)
                {
                    ListFormat format = ListFormat.SourcePortMajor;
                    if (Keywords.Version == FileVersion.Two && Keywords.NumberOfPorts == 2 && Keywords.TwoPortDataOrder.HasValue)
                    {
                        if (Keywords.TwoPortDataOrder.Value == TwoPortDataOrderConfig.TwoOne_OneTwo)
                            format = ListFormat.DestinationPortMajor;
                    }

                    cancelToken.ThrowIfCancellationRequested();

                    ScatteringParametersMatrix matrix = new ScatteringParametersMatrix(parameters, format);
                    pair = new FrequencyParametersPair(frequency, matrix);
                }*/

                throw new NotImplementedException();
            }

            /*protected override Task<FrequencyParametersPair> ReadNextMatrixAsync()
            {
                throw new NotImplementedException();
            }*/
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
