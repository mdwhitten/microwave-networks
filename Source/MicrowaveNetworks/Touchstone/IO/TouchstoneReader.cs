using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

using System.Threading;
using System.Runtime.CompilerServices;
using MicrowaveNetworks.Internal;
using System.Collections;
using MicrowaveNetworks.Matrices;

namespace MicrowaveNetworks.Touchstone.IO
{
    public sealed class TouchstoneReader : IDisposable, IEnumerable<FrequencyParametersPair>

    {
        public TouchstoneKeywords Keywords { get; }
        public TouchstoneOptions Options { get; }
        //public IEnumerable<FrequencyParametersPair> NetworkData => ParseData();
        public IEnumerator<FrequencyParametersPair> GetEnumerator() => ParseData().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public FrequencyParametersPair NetworkData
        {
            get
            {
                if (currentData.HasValue) return currentData.Value;
                else
                {
                    throw new InvalidOperationException($"Either {nameof(ReadData)} has not been called, or the asynchronous read has not yet completed.");
                }
            }
        }


        private static FieldNameLookup<TouchstoneKeywords> keywordLookup = new FieldNameLookup<TouchstoneKeywords>();
        private static string resistanceSignifier = GetTouchstoneFieldName<TouchstoneOptions>(nameof(TouchstoneOptions.Resistance));
        private static string referenceKeywordName = GetTouchstoneFieldName<TouchstoneKeywords>(nameof(TouchstoneKeywords.Reference));


        private TextReader reader;
        private TouchstoneReaderSettings settings;
        private int lineNumber;
        private FrequencyParametersPair? currentData;
        private int? numPorts;
        private int numLinesToRead;

        string previewedLine;

        private TouchstoneReader(TextReader reader, TouchstoneReaderSettings settings)
        {
            this.settings = settings ?? new TouchstoneReaderSettings();
            this.reader = reader;
            Options = new TouchstoneOptions();
            Keywords = new TouchstoneKeywords();
        }
        #region Constructors
        public static TouchstoneReader Create(string filePath) => Create(filePath, new TouchstoneReaderSettings());
        public static TouchstoneReader Create(string filePath, TouchstoneReaderSettings settings)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            StreamReader reader = new StreamReader(filePath);
            return new TouchstoneReader(reader, settings);
        }
        public static TouchstoneReader Create(TextReader reader) => Create(reader, new TouchstoneReaderSettings());
        public static TouchstoneReader Create(TextReader reader, TouchstoneReaderSettings settings)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            return new TouchstoneReader(reader, settings);
        }
        #endregion

        #region Header Parsing
        protected void ReadToNetworkData()
        {
            this.reader = reader;
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            this.reader = reader;

            int nextCharInt;
            bool optionsParsed = false;
            List<char> headerChars = new List<char> { Constants.CommentChar, Constants.OptionChar, Constants.KeywordOpenChar };

            while ((nextCharInt = reader.Peek()) != -1 && headerChars.Contains((char)nextCharInt))
            {
                char nextChar = (char)nextCharInt;
                string line = reader.ReadLine();
                lineNumber++;

                switch (nextChar)
                {
                    case Constants.CommentChar:
                        break;
                    case Constants.OptionChar:
                        // Format specifies that all subsequent option lines should be ignored after first
                        if (!optionsParsed)
                        {
                            line = StripTrailingComment(line);
                            ParseOption(line);
                            optionsParsed = true;
                        }
                        break;
                    case Constants.KeywordOpenChar:
                        line = StripTrailingComment(line);
                        ParseKeyword(line);
                        break;
                }
            }
            // Either EOF or first data line reached. TextReader is now in the position to read the next line on the next call.
            //DetermineNumberOfPorts();
        }
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
            string[] options = Regex.Split(line, @"\s");

            // Skip the first element since it will still contain the "#"
            IEnumerable<string> optionsEnumerable = options.Skip(1);

            // We will manually control the enumerator here since the last item (resistance)
            // has to fetch the next item in sequence
            using (var enumer = optionsEnumerable.GetEnumerator())
            {
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
        }
        #endregion

        private void DetermineNumberOfPorts()
        { }


        public bool ReadData()
        {
            List<string> lines = new List<string>(numLinesToRead);
            if (previewedLine != null) lines.Add(previewedLine);

            if (!numPorts.HasValue)
            {
                if (Keywords.Version == FileVersion.Two) numPorts = Keywords.NumberOfPorts;
                else
                {
                    string currentLine, nextLine;
                    currentLine = ReadNextDataLine();
                    nextLine = ReadNextDataLine();
                    int numColumnsCurrent = TrimAndSplitLine(currentLine).Count;
                    int numColumnsNext = TrimAndSplitLine(currentLine).Count;

                    if (numColumnsCurrent == numColumnsNext)
                    {
                        numPorts = (int)Math.Sqrt((numColumnsCurrent - 1) / 2);
                        lines.Add(currentLine);
                        previewedLine = nextLine;
                    }
                    else
                    {
                        numPorts = (numColumnsCurrent - 1) / 2;
                        lines.Add(currentLine);
                        lines.Add(nextLine);
                    }

                }
                // For two or 1 port files, we only have a single line containing all the data.
                // But for n-port networks, data will span multiple lines
                numLinesToRead = numPorts <= 2 ? 1 : (int)numPorts;
            }

            string line;
            while ((line = ReadNextDataLine()) != null && lines.Count < numLinesToRead)
            {
                lines.Add(line);
            }

            // Make sure we read the expected number of lines rather than reaching EOF
            if (lines.Count == numLinesToRead)
            {
                (bool matchedPredicate, FrequencyParametersPair pair) = ParseLines(lines);
                if (matchedPredicate)
                {
                    currentData = pair;
                    return true;
                }
            }
            return false;
        }

        private string ReadNextDataLine()
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line[0] == Constants.CommentChar) continue;
            }
            return line;
        }
        private async Task<string> ReadNextDataLineAsync()
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                line = line.Trim();
                if (line[0] == Constants.CommentChar) continue;
            }
            return line;
        }

        public async Task<bool> ReadDataAsync()
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                line = line.Trim();
                if (line[0] == Constants.CommentChar) continue;

                (bool matchedPredicate, FrequencyParametersPair pair) = ParseLine(line);
                if (matchedPredicate)
                {
                    currentData = pair;
                    return true;
                }
                else continue;
            }
            return false;
        }


        #region Data Parsing
        /*
#if NET5_0_OR_GREATER
        private async IAsyncEnumerable<FrequencyParametersPair> ParseDataAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                line = line.Trim();
                if (line[0] == Constants.CommentChar) continue;

                (bool matchedPredicate, FrequencyParametersPair pair) = ParseLine(line, token);
                if (matchedPredicate)
                {
                    yield return pair;
                }
            }
        }
#endif

        private IEnumerable<FrequencyParametersPair> ParseData()
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line[0] == Constants.CommentChar) continue;

                (bool matchedPredicate, FrequencyParametersPair pair) = ParseLine(line);
                if (matchedPredicate)
                {
                    yield return pair;
                }
            }
        }*/
        private (bool, FrequencyParametersPair) ParseLines(List<string> lines, CancellationToken cancelToken = default)
        {
            FrequencyParametersPair pair = default;
            double frequency = 0;
            bool selected = false;

            List<NetworkParameter> parameters = new List<NetworkParameter>();

            // Loop through multiple lines for matrices of order 3 and greater
            for (int i = 0; i < lines.Count; i++)
            {
                string currentLine = lines[i];
                List<string> data = TrimAndSplitLine(currentLine);

                // The first line will always include the frequency
                if (i == 0)
                {
                    string frequencyString = data[0];

                    (selected, frequency) = ProcessFrequency(frequencyString);
                    if (!selected) break;

                    data.RemoveAt(0);
                }
                parameters.AddRange(ProcessParameters(data, cancelToken));

            }
            if (selected)
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
            }


            return (selected, pair);
        }

        private List<NetworkParameter> ProcessParameters(List<string> data, CancellationToken cancelToken)
        {
            List<NetworkParameter> parameters = new List<NetworkParameter>();
            int dataLength = data.Count / 2;

            if (!dataLength.IsPerfectSquare(out int ports))
                ThrowHelper("Data", "Invalid data format");

            cancelToken.ThrowIfCancellationRequested();

            if (settings.ParameterSelector != null)
                throw new NotImplementedException();

            for (int j = 1; j < data.Count; j += 2)
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

        private (bool Selected, double Frequency) ProcessFrequency(string frequencyString)
        {
            (bool Selected, double Frequency) result;
            bool success = double.TryParse(frequencyString, out double frequency);
            if (!success) ThrowHelper("Data", "Invalid format for frequency");

            frequency *= Options.FrequencyUnit.GetMultiplier();
            bool selectedFrequency = true;

            if (settings.FrequencySelector != null)
            {
                try
                {
                    selectedFrequency = settings.FrequencySelector(frequency);
                }
                catch
                {
                    // Assume any exception means we should not use this frequency
                    selectedFrequency = false;
                }
            }
            result = (selectedFrequency, frequency);
            return result;
        }

        /*
        private (bool, FrequencyParametersPair) ParseLine(string line, CancellationToken cancelToken = default)
        {




            // # TODO: Support n port files

            // Exclude the first element (frequency) and divide by two since there should be two values per port
            int adjustedDataLength = (data.Length - 1) / 2;

            if (!adjustedDataLength.IsPerfectSquare(out int ports))
                ThrowHelper("Data", "Invalid data format");

            bool success = double.TryParse(data[0], out double frequency);
            if (!success) ThrowHelper("Data", "Invalid format for frequency");

            frequency *= Options.FrequencyUnit.GetMultiplier();

            bool selectedFrequency = true;
            if (settings.FrequencySelector != null)
            {
                try
                {
                    selectedFrequency = settings.FrequencySelector(frequency);
                }
                catch
                {
                    // Assume any exception means we should not use this frequency
                    selectedFrequency = false;
                }
            }
            if (selectedFrequency)
            {
                matchedPredicate = true;

                List<NetworkParameter> parameters = new List<NetworkParameter>();

                cancelToken.ThrowIfCancellationRequested();

                if (settings.ParameterSelector != null)
                    throw new NotImplementedException();

                for (int i = 1; i < data.Length; i += 2)
                {
                    double val1 = 0, val2 = 0;
                    try
                    {
                        val1 = double.Parse(data[i]);
                        val2 = double.Parse(data[i + 1]);
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

                ListFormat format = ListFormat.SourcePortMajor;
                if (ports == 2 && Keywords.TwoPortDataOrder.HasValue)
                {
                    if (Keywords.TwoPortDataOrder.Value == TwoPortDataOrderConfig.TwoOne_OneTwo)
                        format = ListFormat.DestinationPortMajor;
                }

                cancelToken.ThrowIfCancellationRequested();

                ScatteringParametersMatrix matrix = new ScatteringParametersMatrix(parameters, format);

                pair = new FrequencyParametersPair(frequency, matrix);
            }

            return (matchedPredicate, pair);
        }
        */
        private static List<string> TrimAndSplitLine(string line)
        {
            // Remove any trailing comments and any leading or trailing whitespace
            line = StripTrailingComment(line).Trim();

            string[] data = Regex.Split(line, @"\s");
            return new List<string>(data);
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
        private static T StringToEnum<T>(string value) where T : Enum
        {
            return (T)Enum.Parse(typeof(T), value);
        }
        #endregion
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected void Dispose(bool disposing)
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
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
