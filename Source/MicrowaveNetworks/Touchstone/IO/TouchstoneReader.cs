using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public abstract class TouchstoneReader : IDisposable, IEnumerable<FrequencyParametersPair>
    {
        public TouchstoneKeywords Keywords { get; }
        public TouchstoneOptions Options { get; }
        public IEnumerable<FrequencyParametersPair> NetworkData => ParseData();
        public IEnumerator<FrequencyParametersPair> GetEnumerator() => ParseData().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static FieldNameLookup<TouchstoneKeywords> keywordLookup = new FieldNameLookup<TouchstoneKeywords>();
        private static FieldNameLookup<FrequencyUnit> frequencyUnitLookup = new FieldNameLookup<FrequencyUnit>();
        private static FieldNameLookup<ParameterType> parameterTypeLookup = new FieldNameLookup<ParameterType>();
        private static FieldNameLookup<FormatType> formatTypeLookup = new FieldNameLookup<FormatType>();
        private static Lazy<string> resistanceSignifier = new Lazy<string>(()
            => GetTouchstoneFieldName<TouchstoneOptions>(nameof(TouchstoneOptions.Resistance)));
        private static Lazy<string> referenceKeywordName = new Lazy<string>(()
            => GetTouchstoneFieldName<TouchstoneKeywords>(nameof(TouchstoneKeywords.Reference)));


        private TextReader reader;
        private TouchstoneReaderSettings settings;
        //private Touchstone file;
        private int lineNumber;

        protected TouchstoneReader(TouchstoneReaderSettings settings)
        {
            this.settings = settings;
            Options = new TouchstoneOptions();
            Keywords = new TouchstoneKeywords();
        }
        private string StripTrailingComment(string line)
        {
            int index = line.IndexOf(Constants.CommentChar);
            if (index >= 0)
            {
                return line.Substring(0, index);
            }
            else return line;
        }
        #region Header Parsing
        protected void ParseToData(TextReader reader)
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
            else if (match.Groups[1].Value == referenceKeywordName.Value)
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
                    else if (option == resistanceSignifier.Value)
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
        #region Data Parsing
#if NETSTANDARDALL
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
        }
        private (bool, FrequencyParametersPair) ParseLine(string line, CancellationToken cancelToken = default)
        {
            FrequencyParametersPair pair = default;
            bool matchedPredicate = false;

            // Remove any trailing comments and any leading or trailing whitespace
            line = StripTrailingComment(line);
            line = line.Trim();
            
            string[] data = Regex.Split(line, @"\s");

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
        #endregion

        public static TouchstoneReader CreateWithFile(string filePath) => CreateWithFile(filePath, new TouchstoneReaderSettings());
        public static TouchstoneReader CreateWithFile(string filePath, TouchstoneReaderSettings settings) => new TouchstoneFileReader(filePath, settings);
        public static TouchstoneReader CreateWithString(string fileText) => CreateWithString(fileText, new TouchstoneReaderSettings());
        public static TouchstoneReader CreateWithString(string fileText, TouchstoneReaderSettings settings) => new TouchstoneStringReader(fileText, settings);


        #region Utilities
        private static string GetTouchstoneFieldName<T>(string objectFieldName)
        {
            FieldInfo f = typeof(T).GetField(objectFieldName);
            var attr = f.GetCustomAttribute<TouchstoneParameterAttribute>();
            return attr.FieldName;
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

        protected virtual void Dispose(bool disposing)
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
