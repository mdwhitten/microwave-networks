using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TouchstoneSnPFileReader
{
    using ScatteringParameters;

    internal class FieldLookup<T>
    {
        private Lazy<Dictionary<string, string>> fieldLookup;

        public Dictionary<string, string> Value => fieldLookup.Value;

        public FieldLookup()
        {
            fieldLookup = new Lazy<Dictionary<string, string>>(() => CreateFieldLookup());
        }

        private static Dictionary<string, string> CreateFieldLookup()
        {
            var keywordLookup = new Dictionary<string, string>();
            var fields = typeof(T).GetFields();
            foreach (var field in fields)
            {
                string fieldName;
                if (field.IsDefined(typeof(TouchstoneParameterAttribute)))
                {
                    var attr = field.GetCustomAttribute<TouchstoneParameterAttribute>();
                    fieldName = attr.FieldName;
                }
                else fieldName = field.Name;

                keywordLookup.Add(fieldName, field.Name);
            }
            return keywordLookup;
        }
    }

    public class TouchstoneParser : IDisposable
    {
        private const char CommentChar = '!';
        private const char OptionChar = '#';
        private const char KeywordChar = '[';

        private static FieldLookup<TouchstoneFileKeywords> keywordLookup = new FieldLookup<TouchstoneFileKeywords>();
        private static FieldLookup<FrequencyUnit> frequencyUnitLookup = new FieldLookup<FrequencyUnit>();
        private static FieldLookup<ParameterType> parameterTypeLookup = new FieldLookup<ParameterType>();
        private static FieldLookup<FormatType> formatTypeLookup = new FieldLookup<FormatType>();
        private static Lazy<string> resistanceSignifier = new Lazy<string>(() =>
        {
            FieldInfo f = typeof(TouchstoneFileOptions).GetField(nameof(TouchstoneFileOptions.Resistance));
            var attr = f.GetCustomAttribute<TouchstoneParameterAttribute>();
            return attr.FieldName;
        });


        private TextReader reader;
        private TouchstoneFile file;
        private int lineNumber;

        public TouchstoneParser(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            this.reader = reader;
            file = new TouchstoneFile();
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
        public async Task<TouchstoneFile> ParseAsync() => await ParseAsync(CancellationToken.None);
        public async Task<TouchstoneFile> ParseAsync(CancellationToken cancelToken)
        {
            string line;
            bool optionsParsed = false;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancelToken.ThrowIfCancellationRequested();

                lineNumber++;
                char firstChar = line[0];

                switch (firstChar)
                {
                    case CommentChar:
                        break;
                    case OptionChar:
                        // Format specifies that all subsequent option lines should be ignored after first
                        if (!optionsParsed)
                        {
                            ParseOption(line);
                            optionsParsed = true;
                        }
                        break;
                    case KeywordChar:
                        await ParseKeyword(line);
                        break;
                    default:
                        await ParseData(line, cancelToken);
                        break;
                }
            }

            // If we never found the 2.0 version keyword, file is necessarily 1.0 file
            if (file.Keywords.Version == null) file.Keywords.Version = FileVersion.One;

            return file;
        }
        public TouchstoneFile Parse() => ParseAsync().Result;
        private async Task ParseData(string line, CancellationToken cancelToken)
        {
            line = line.TrimStart();
            string[] data = Regex.Split(line, @"\s");
            int ports = 0;

            if (file.ScatteringParameters == null)
            {
                ports = (int)Math.Sqrt((data.Length - 1) / 2);
                file.ScatteringParameters = new ScatteringParametersCollection(ports);
            }
            double frequency = double.Parse(data[0]);
            List<ScatteringParameter> parameters = new List<ScatteringParameter>();

            cancelToken.ThrowIfCancellationRequested();

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
                ScatteringParameter s = new ScatteringParameter();
                switch (file.Options.Format)
                {
                    case FormatType.DecibelAngle:
                        s = ScatteringParameter.FromMagnitudeDecibelAngle(val1, val2);
                        break;
                    case FormatType.MagnitudeAngle:
                        s = ScatteringParameter.FromMagnitudeAngle(val1, val2);
                        break;
                    case FormatType.RealImaginary:
                        s = new ScatteringParameter(val1, val2);
                        break;
                }
                parameters.Add(s);
            }

            ListFormat format = ListFormat.SourcePortMajor;
            if (ports == 2 && file.Keywords.TwoPortDataOrder.HasValue)
            {
                if (file.Keywords.TwoPortDataOrder.Value == TwoPortDataOrderConfig.TwoOne_OneTwo)
                    format = ListFormat.DestinationPortMajor;
            }

            cancelToken.ThrowIfCancellationRequested();

            file.ScatteringParameters[frequency] = new ScatteringParametersMatrix(parameters, format);
        }

        private async Task ParseKeyword(string line)
        {
            var match = Regex.Match(line, @"[(\w+)]\s(\w+)?");

            if (!match.Success) ThrowHelper("Keywords", "Bad keyword format");

            if (match.Groups[2].Success)
            {
                string keywordName = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                bool found = keywordLookup.Value.TryGetValue(keywordName, out string fieldName);
                if (!found) ThrowHelper("Keywords", "Unknown keyword");

                FieldInfo field = typeof(TouchstoneFileKeywords).GetField(fieldName);
                object convertedValue = null;
                try
                {
                    convertedValue = Convert.ChangeType(value, field.FieldType);
                }
                catch (Exception ex) when (ex is InvalidCastException || ex is FormatException)
                {
                    ThrowHelper("Keywords", "Bad keyword value", ex);
                }

                field.SetValue(file.Keywords, convertedValue);
            }
            else throw new NotImplementedException();
        }

        private T StringToEnum<T>(string value) where T : Enum
        {
            return (T)Enum.Parse(typeof(T), value);
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
                    if (frequencyUnitLookup.Value.ContainsKey(option))
                    {
                        string frequencyUnitName = frequencyUnitLookup.Value[option];
                        file.Options.FrequencyUnit = StringToEnum<FrequencyUnit>(frequencyUnitName);
                    }
                    else if (formatTypeLookup.Value.ContainsKey(option))
                    {
                        string formatTypeName = formatTypeLookup.Value[option];
                        file.Options.Format = StringToEnum<FormatType>(formatTypeName);
                    }
                    else if (parameterTypeLookup.Value.ContainsKey(option))
                    {
                        string parameterTypeName = parameterTypeLookup.Value[option];
                        file.Options.Parameter = StringToEnum<ParameterType>(parameterTypeName);
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
                            if (parsed) file.Options.Resistance = r;
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
                // TODO: set large fields to null.
                disposedValue = true;
            }
        }
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }


}
