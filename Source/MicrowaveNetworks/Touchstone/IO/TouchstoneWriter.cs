using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using static MicrowaveNetworks.Internal.Utilities;
using static MicrowaveNetworks.Touchstone.IO.Constants;
using System.Text.RegularExpressions;
using MicrowaveNetworks.Matrices;
using System.Threading;

namespace MicrowaveNetworks.Touchstone.IO
{
    public abstract class TouchstoneWriter : IDisposable
    {
        public TouchstoneOptions Options { get; set; } = new TouchstoneOptions();
        public TouchstoneKeywords Keywords { get; set; } = new TouchstoneKeywords();
        public CancellationToken CancelToken { get; set; } = default;

        protected TextWriter Writer { get; set; }

        TouchstoneWriterSettings settings;
        bool headerWritten;
        bool columnsWritten;

        protected TouchstoneWriter(TouchstoneWriterSettings settings)
        {
            this.settings = settings;
        }
        protected TouchstoneWriter(TouchstoneWriterSettings settings, TouchstoneOptions options)
        {
            this.Options = options;
            this.settings = settings;
        }
        private static string FormatOptions(TouchstoneOptions options)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(OptionChar);

            string frequencyUnit = TouchstoneEnumMap<FrequencyUnit>.ToTouchstoneValue(options.FrequencyUnit);
            string parameter = TouchstoneEnumMap<ParameterType>.ToTouchstoneValue(options.Parameter);
            string format = TouchstoneEnumMap<FormatType>.ToTouchstoneValue(options.Format);
            string resistance = $"{ResistanceChar} {options.Resistance:g}";

            return string.Join(" ", OptionChar, frequencyUnit, parameter, format, resistance);
        }
        private static string FormatComment(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                throw new ArgumentNullException(nameof(comment));

            // If comment spans multiple lines, a comment character is necessary at each line
            string[] lines = Regex.Split(comment, "[\n\f\r]+");

            // Add the comment character and ensure that it is trimmed if present
            var commentLines = lines.Select(l => CommentChar + " " + l.Trim(CommentChar, ' '));
            return string.Join(Environment.NewLine, commentLines);
        }
        private string FormatColumns(int numberOfPorts)
        {
            List<string> columns = new List<string>();

            string frequencyUnit = $"Frequency ({Options.FrequencyUnit})";
            frequencyUnit = frequencyUnit.PadRight(settings.ColumnWidth - 2);

            columns.Add(frequencyUnit);

            string parameter = TouchstoneEnumMap<ParameterType>.ToTouchstoneValue(Options.Parameter);
            string description1 = null, description2 = null;
            switch (Options.Format)
            {
                case FormatType.DecibelAngle:
                    description1 = "Mag (dB)";
                    description2 = "Angle (deg)";
                    break;
                case FormatType.MagnitudeAngle:
                    description1 = "Mag";
                    description2 = "Angle (deg)";
                    break;
                case FormatType.RealImaginary:
                    description1 = "Real";
                    description2 = "Imag";
                    break;
            }

            int leftPad = (int)Math.Floor(settings.ColumnWidth / 2.0);
            int rightPad = (int)Math.Ceiling(settings.ColumnWidth / 2.0);

            var result = ForEachParameter(numberOfPorts, indices =>
            {
                (int dest, int source) = indices;
                string column1 = $"{parameter}{dest}{source}:{description1}";
                string column2 = $"{parameter}{dest}{source}:{description2}";

                column1 = column1.PadRight(settings.ColumnWidth);
                column2 = column2.PadRight(settings.ColumnWidth);
                return column1 + "\t" + column2;
            });

            columns.AddRange(result);

            return string.Join("\t", columns);
        }

        private string FormatEntry(double frequency, NetworkParametersMatrix matrix)
        {
            //StringBuilder builder = new StringBuilder();
            string formatString = settings.NumericFormatString;
            if (string.IsNullOrEmpty(settings.NumericFormatString))
            {
                formatString = "0.00000000000E+00";
            }
            IFormatProvider provider = settings.NumericFormatProvider ?? CultureInfo.CurrentCulture.NumberFormat;

            double scaledFrequency = frequency / Options.FrequencyUnit.GetMultiplier();
            //string frequencyStr = scaledFrequency.ToString(formatString, provider);
            //builder.Append(frequen)

            List<double> numbersToFormat = new List<double>();
            numbersToFormat.Add(scaledFrequency);

            foreach (var (_, parameter) in matrix)
            {
                switch (Options.Format)
                {
                    case FormatType.DecibelAngle:
                        numbersToFormat.Add(parameter.Magnitude_dB);
                        numbersToFormat.Add(parameter.Phase_deg);
                        break;
                    case FormatType.MagnitudeAngle:
                        numbersToFormat.Add(parameter.Magnitude);
                        numbersToFormat.Add(parameter.Phase_deg);
                        break;
                    case FormatType.RealImaginary:
                        numbersToFormat.Add(parameter.Real);
                        numbersToFormat.Add(parameter.Imaginary);
                        break;
                }
            }
            string width = string.Empty;
            if (settings.UnifiedColumnWidth) width = settings.ColumnWidth + ":";
            string compositeFormatString = $"{{0,{width}{formatString}}}";
            var values = numbersToFormat.Select(d => string.Format(compositeFormatString, d));

            string line = string.Join("\t", values).Trim();
            return line;
        }
        
        public void WriteHeader()
        {
            string options = FormatOptions(Options);
            Writer.WriteLine(options);

            if (Keywords.Version.HasValue && Keywords.Version.Value == FileVersion.Two)
            {
                WriteKeywords();
            }
            headerWritten = true;
        }
        public async Task WriteHeaderAsync()
        {
            string options = FormatOptions(Options);
            await Writer.WriteLineAsync(options);

            if (Keywords.Version.HasValue && Keywords.Version.Value == FileVersion.Two)
            {
                WriteKeywords();
            }
            headerWritten = true;
        }
        private void WriteKeywords()
        {
            throw new NotImplementedException();
        }
        public void WriteEntry(double frequency, NetworkParametersMatrix matrix)
        {
            if (!headerWritten) WriteHeader();
            if (settings.IncludeColumnNames && !columnsWritten)
            {
                string columns = FormatColumns(matrix.NumPorts);
                WriteCommentLine(columns);
                columnsWritten = true;
            }
            string line = FormatEntry(frequency, matrix);
            Writer.WriteLine(line);
        }
        public void WriteEntry(FrequencyParametersPair pair) => WriteEntry(pair.Frequency_Hz, pair.Parameters);
        public async Task WriteEntryAsync(double frequency, NetworkParametersMatrix matrix)
        {
            if (!headerWritten) await WriteHeaderAsync();
            
            CancelToken.ThrowIfCancellationRequested();

            if (settings.IncludeColumnNames && !columnsWritten)
            {
                string columns = FormatColumns(matrix.NumPorts);
                await WriteCommentLineAsync(columns);
                columnsWritten = true;

                CancelToken.ThrowIfCancellationRequested();
            }
            string line = FormatEntry(frequency, matrix);
            await Writer.WriteLineAsync(line);
        }
        public async Task WriteEntryAsync(FrequencyParametersPair pair) => await WriteEntryAsync(pair.Frequency_Hz, pair.Parameters);

        public void WriteCommentLine(string comment)
        {
            string formattedCommment = FormatComment(comment);
            Writer.WriteLine(formattedCommment);
        }
        public async Task WriteCommentLineAsync(string comment)
        {
            string formattedCommment = FormatComment(comment);
            await Writer.WriteLineAsync(formattedCommment);
        }

        public void Flush() => Writer.Flush();
        public async Task FlushAsync() => await Writer.FlushAsync();

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Writer?.Dispose();
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
