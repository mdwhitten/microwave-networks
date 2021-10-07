using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using static MicrowaveNetworks.Internal.Utilities;
using static MicrowaveNetworks.Touchstone.IO.Constants;

namespace MicrowaveNetworks.Touchstone.IO
{
    /// <summary>
    /// Provides lower-level support for rendering Touchstone files from network data and Touchstone options and keywords.
    /// </summary>
    public sealed class TouchstoneWriter : IDisposable
#if NET5_0_OR_GREATER
                                    , IAsyncDisposable
#endif
    {
        /// <summary>The <see cref="TouchstoneOptions"/> that will be used to form the options line in the resulting file as well as the units and data types
        /// of the network data.</summary>
        public TouchstoneOptions Options { get; set; } = new TouchstoneOptions();
        /// <summary>The <see cref="TouchstoneKeywords"/> that will be used to write keywords for <see cref="FileVersion.Two"/> file types.
        /// For <see cref="FileVersion.One"/> files (the default), these keywords are ignored as they are not valid per the specification.</summary>
        public TouchstoneKeywords Keywords { get; set; } = new TouchstoneKeywords();
        /// <summary>The cancellation token to cancel operations if using the asynchronous write functions.</summary>
        public CancellationToken CancelToken { get; set; } = default;

        private TextWriter Writer { get; set; }

        readonly TouchstoneWriterSettings settings;
        bool headerWritten;
        bool columnsWritten;
        TouchstoneWriterCore core;

        private static readonly FieldNameLookup<TouchstoneKeywords> keywordLookup = new FieldNameLookup<TouchstoneKeywords>();

        private TouchstoneWriter(TextWriter writer, TouchstoneWriterSettings settings)
        {
            this.settings = settings ?? new TouchstoneWriterSettings();
            this.Writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        #region Static Constructors
        /// <summary>
        /// Creates a new <see cref="TouchstoneWriter"/> using the specified file path with default <see cref="TouchstoneWriterSettings"/>.
        /// </summary>
        /// <param name="filePath">The file to which you want to write. The <see cref="TouchstoneWriter"/> creates a file at the specified path
        /// or overwrites the existing file.</param>
        /// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="filePath"/> value is null.</exception>
        public static TouchstoneWriter Create(string filePath) => Create(filePath, new TouchstoneWriterSettings());
        /// <summary>
        /// Creates a new <see cref="TouchstoneWriter"/> using the specified file path with the specified settings.
        /// </summary>
        /// <param name="filePath">The file to which you want to write. The <see cref="TouchstoneWriter"/> creates a file at the specified path
        /// or overwrites the existing file.</param>
        /// <param name="settings">The <see cref="TouchstoneWriterSettings"/> used to configure the <see cref="TouchstoneWriter"/> instance. 
        /// If <paramref name="settings"/> is null the default settings will be used.</param>
        /// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
        public static TouchstoneWriter Create(string filePath, TouchstoneWriterSettings settings)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            StreamWriter writer = new StreamWriter(filePath);
            return new TouchstoneWriter(writer, settings);
        }
        /// <summary>
        /// Creates a new <see cref="TouchstoneWriter"/> using the specified <see cref="TextWriter"/> with default <see cref="TouchstoneWriterSettings"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to which you want to write. The Touchstone data will be appended to this <see cref="TextWriter"/>.</param>
        /// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="writer"/> value is null.</exception>
        public static TouchstoneWriter Create(TextWriter writer) => Create(writer, new TouchstoneWriterSettings());
        /// <summary>
        /// Creates a new <see cref="TouchstoneWriter"/> using the specified <see cref="TextWriter"/> with default <see cref="TouchstoneWriterSettings"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> to which you want to write. The Touchstone data will be appended to this <see cref="TextWriter"/>.</param>
        /// <param name="settings">The <see cref="TouchstoneWriterSettings"/> used to configure the <see cref="TouchstoneWriter"/> instance. 
        /// If <paramref name="settings"/> is null the default settings will be used.</param>
        /// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="writer"/> value is null.</exception>
        public static TouchstoneWriter Create(TextWriter writer, TouchstoneWriterSettings settings)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            return new TouchstoneWriter(writer, settings);
        }
        /// <summary>
        /// Creates a new <see cref="TouchstoneWriter"/> using the specified <see cref="StringBuilder"/> with default <see cref="TouchstoneWriterSettings"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to which you want to write. The Touchstone data will be appended to this <see cref="StringBuilder"/>.</param>
        /// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="sb"/> value is null.</exception>
        public static TouchstoneWriter Create(StringBuilder sb) => Create(sb, new TouchstoneWriterSettings());
        /// <summary>
        /// Creates a new <see cref="TouchstoneWriter"/> using the specified <see cref="StringBuilder"/> with default <see cref="TouchstoneWriterSettings"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to which you want to write. The Touchstone data will be appended to this <see cref="StringBuilder"/>.</param>
        /// <param name="settings">The <see cref="TouchstoneWriterSettings"/> used to configure the <see cref="TouchstoneWriter"/> instance. 
        /// If <paramref name="settings"/> is null the default settings will be used.</param>
        /// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="sb"/> value is null.</exception>
        public static TouchstoneWriter Create(StringBuilder sb, TouchstoneWriterSettings settings)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));
            StringWriter writer = new StringWriter(sb);
            return new TouchstoneWriter(writer, settings);
        }
        #endregion
        #region Internal Formatting Functions
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
        //private static string FormatKeyword()
        private string FormatColumns(int numberOfPorts)
        {
            ListFormat format = core.GetListFormat(numberOfPorts);

            string frequencyDescription = $"Frequency ({Options.FrequencyUnit})";
            string columnPad = string.Empty;
            if (settings.UnifiedColumnWidth)
            {
                frequencyDescription = frequencyDescription.PadRight(settings.ColumnWidth - 2);
                columnPad = columnPad.PadRight(settings.ColumnWidth - 2);
            }

            //columns.Add(frequencyUnit);

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

            /*int leftPad = (int)Math.Floor(settings.ColumnWidth / 2.0);
            int rightPad = (int)Math.Ceiling(settings.ColumnWidth / 2.0);*/

            List<string> parameterDescriptions = new List<string>();

            ForEachParameter(numberOfPorts, format, indices =>
            {
                (int dest, int source) = indices;

                string column1 = $"{parameter}{dest}{source}:{description1}";
                string column2 = $"{parameter}{dest}{source}:{description2}";

                if (settings.UnifiedColumnWidth)
                {
                    column1 = column1.PadRight(settings.ColumnWidth);
                    column2 = column2.PadRight(settings.ColumnWidth);
                }

                parameterDescriptions.Add(column1);
                parameterDescriptions.Add(column2);
            });

            string formattedColumns = FormatMatrix(frequencyDescription, columnPad, parameterDescriptions, numberOfPorts);

            return formattedColumns;
        }
        private static string FormatComment(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                throw new ArgumentNullException(nameof(comment));

            // If comment spans multiple lines, a comment character is necessary at each line
            string[] lines = Regex.Split(comment, "[\n\f\r]+");

            // Add the comment character and ensure that it is trimmed if present
            var commentLines = lines.Select(l => CommentChar + " " + l.Trim(CommentChar));
            return string.Join(Environment.NewLine, commentLines);
        }
        private string FormatEntry(double frequency, NetworkParametersMatrix matrix)
        {
            string formatString = settings.NumericFormatString;
            if (string.IsNullOrEmpty(settings.NumericFormatString))
            {
                formatString = "0.00000000000E+00";
            }
            IFormatProvider provider = settings.NumericFormatProvider ?? CultureInfo.CurrentCulture.NumberFormat;

            string width = string.Empty, frequencyWidth = string.Empty;
            string columnPad = string.Empty;
            if (settings.UnifiedColumnWidth)
            {
                width = settings.ColumnWidth + ":";
                frequencyWidth = -settings.ColumnWidth + ":";
                columnPad = string.Empty.PadRight(settings.ColumnWidth);
            }
            string compositeFormatString = $"{{0,{width}{formatString}}}";
            string frequencyCompositeString = $"{{0,{frequencyWidth}{formatString}}}";

            int numPorts = matrix.NumPorts;
            ListFormat format = core.GetListFormat(numPorts);

            List<double> parameters = new List<double>();

            // Prepare frequency
            double scaledFrequency = frequency / Options.FrequencyUnit.GetMultiplier();

            foreach (var (_, parameter) in matrix.EnumerateParameters(format))
            {
                switch (Options.Format)
                {
                    case FormatType.DecibelAngle:
                        parameters.Add(parameter.Magnitude_dB);
                        parameters.Add(parameter.Phase_deg);
                        break;
                    case FormatType.MagnitudeAngle:
                        parameters.Add(parameter.Magnitude);
                        parameters.Add(parameter.Phase_deg);
                        break;
                    case FormatType.RealImaginary:
                        parameters.Add(parameter.Real);
                        parameters.Add(parameter.Imaginary);
                        break;
                }
            }

            string frequencyString = string.Format(settings.NumericFormatProvider, frequencyCompositeString, scaledFrequency);
            var parametersString = parameters.Select(p => string.Format(settings.NumericFormatProvider, compositeFormatString, p));

            string formattedEntry = FormatMatrix(frequencyString, columnPad, parametersString, numPorts);

            return formattedEntry;
        }
        private string FormatMatrix(string firstValue, string spaceValue, IEnumerable<string> entries, int numPorts)
        {
            int maxDataPairs = core.GetNumberOfDataPairsPerLine(numPorts);
            int maxNumColumns = (maxDataPairs * 2) + 1;

            ListFormat format = core.GetListFormat(numPorts);
            StringBuilder sb = new StringBuilder(firstValue);
            int previousDestinationPort = 1;
            int currentColumn = 1;

            using var enumer = entries.GetEnumerator();

            ForEachParameter(numPorts, format, index =>
            {
                if (currentColumn == maxNumColumns ||
                            (numPorts > 2 && index.DestinationPort > previousDestinationPort))
                {
                    sb.AppendLine();
                    sb.Append(spaceValue);
                    currentColumn = 1;
                    previousDestinationPort = index.DestinationPort;
                }

                enumer.MoveNext();
                sb.Append('\t');
                sb.Append(enumer.Current);
                enumer.MoveNext();
                sb.Append('\t');
                sb.Append(enumer.Current);
                currentColumn += 2;
            });
            return sb.ToString();
        }

        private static Dictionary<string, string> FormatKeywords(TouchstoneKeywords keywords)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region Public Write Functions
        /// <summary>Writes the <see cref="Options"/> object to the options line in the Touchstone file. If <see cref="TouchstoneKeywords.Version"/> in 
        /// <see cref="Keywords"/> is <see cref="FileVersion.Two"/>, the keywords will also be written to the file.</summary>
        /// <remarks>This method may only be called once for a file. If it is not called explicitly, the first call to <see cref="WriteData(double, NetworkParametersMatrix)"/>
        /// will implicitly call this method.</remarks>
        public void WriteHeader()
        {
            if (headerWritten) throw new InvalidOperationException("The header can only be written once.");
            core = TouchstoneWriterCore.Create(this);

            core.WriteHeader();

            headerWritten = true;
        }
        /// <summary>Asynchronously writes the <see cref="Options"/> object to the options line in the Touchstone file. If <see cref="TouchstoneKeywords.Version"/> in 
        /// <see cref="Keywords"/> is <see cref="FileVersion.Two"/>, the keywords will also be written to the file.</summary>
        /// <remarks>This method may only be called once for a file. If it is not called explicitly, the first call to <see cref="WriteDataAsync(double, NetworkParametersMatrix)"/>
        /// will implicitly call this method.</remarks>
        public async Task WriteHeaderAsync()
        {
            if (headerWritten) throw new InvalidOperationException("The header can only be written once.");
            core = TouchstoneWriterCore.Create(this);

            await core.WriteHeaderAsync();

            headerWritten = true;
            headerWritten = true;
        }
        private void WriteKeywords()
        {
            throw new NotImplementedException();
        }
        /// <summary>Writes the frequency and <see cref="NetworkParametersMatrix"/> contained in <paramref name="pair"/> to the network data of the file.</summary>
        /// <param name="pair">The <see cref="NetworkParametersMatrix"/> and corresponding frequency to write to the Touchstone file.</param>
        /// <remarks>If <see cref="WriteHeader"/> has not yet been called, this method will be called automatically before writing any network data.</remarks>
        public void WriteData(FrequencyParametersPair pair) => WriteData(pair.Frequency_Hz, pair.Parameters);
        /// <summary>Writes the frequency and <see cref="NetworkParametersMatrix"/> the network data of the file.</summary>
        /// <param name="frequency">The frequency of the network data to be written.</param>
        /// /// <param name="matrix">The network data to be written.</param>
        /// <remarks>If <see cref="WriteHeader"/> has not yet been called, this method will be called automatically before writing any network data.</remarks>
        public void WriteData(double frequency, NetworkParametersMatrix matrix)
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

        /// <summary>Asynchronously writes the frequency and <see cref="NetworkParametersMatrix"/> contained in <paramref name="pair"/> to the network data of the file.</summary>
        /// <param name="pair">The <see cref="NetworkParametersMatrix"/> and corresponding frequency to write to the Touchstone file.</param>
        /// <remarks>If <see cref="WriteHeaderAsync"/> has not yet been called, this method will be called automatically before writing any network data.</remarks>
        public async Task WriteDataAsync(FrequencyParametersPair pair) => await WriteDataAsync(pair.Frequency_Hz, pair.Parameters);
        /// <summary>Asynchronously writes the frequency and <see cref="NetworkParametersMatrix"/> the network data of the file.</summary>
        /// <param name="frequency">The frequency of the network data to be written.</param>
        /// /// <param name="matrix">The network data to be written.</param>
        /// <remarks>If <see cref="WriteHeaderAsync"/> has not yet been called, this method will be called automatically before writing any network data.</remarks>
        public async Task WriteDataAsync(double frequency, NetworkParametersMatrix matrix)
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
        /// <summary>Appends a comment line, preceeded with '!', to the Touchstone file.</summary>
        /// <param name="comment">The comment to be appended. If <paramref name="comment"/> has more than one line, each line will be prepended with the comment character.</param>
        public void WriteCommentLine(string comment)
        {
            string formattedCommment = FormatComment(comment);
            Writer.WriteLine(formattedCommment);
        }
        /// <summary>Asynchronously appends a comment line, preceeded with '!', to the Touchstone file.</summary>
        /// <param name="comment">The comment to be appended. If <paramref name="comment"/> has more than one line, each line will be prepended with the comment character.</param>
        public async Task WriteCommentLineAsync(string comment)
        {
            string formattedCommment = FormatComment(comment);
            await Writer.WriteLineAsync(formattedCommment);
        }
        /// <summary>Invokes <see cref="TextWriter.Flush"/> on the underlying object to clear the buffers and cause all data to be written.</summary>
        public void Flush() => Writer.Flush();
        /// <summary>Invokes <see cref="TextWriter.FlushAsync"/> on the underlying object to asynchronously clear the buffers and cause all data to be written.</summary>
        public async Task FlushAsync() => await Writer.FlushAsync();
        #endregion
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Specification requires an [End] keyword at the end of the file
                    if (Keywords.Version == FileVersion.Two)
                    {
                        Writer?.Write(ControlKeywords.End);
                    }
                    Writer?.Dispose();
                }

                disposedValue = true;
            }
        }
        /// <summary>
        /// Disposes the underlying <see cref="TextWriter"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
#if NET5_0_OR_GREATER
        private async ValueTask DisposeAsyncCore()
        {
            if (Writer != null)
            {
                // Specification requires an [End] keyword at the end of the file
                if (Keywords.Version == FileVersion.Two)
                {
                    await Writer.WriteAsync(ControlKeywords.End);
                }
                await Writer.DisposeAsync();
            }
        }
        /// <summary>
        /// Asynchronously disposes the underlying <see cref="TextWriter"/> object.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            // Perform async cleanup.
            await DisposeAsyncCore();

            // Dispose of unmanaged resources.
            Dispose(false);
        }
#endif
        #endregion

        #region Core Classes
        abstract class TouchstoneWriterCore
        {
            protected TouchstoneWriter tsWriter;

            public virtual int GetNumberOfDataPairsPerLine(int numPorts)
            {
                if (numPorts <= 2)
                {
                    return 4;
                }
                else return numPorts;
            }

            public abstract ListFormat GetListFormat(int numPorts);

            public abstract void WriteHeader();
            public abstract Task WriteHeaderAsync();

            protected TouchstoneWriterCore(TouchstoneWriter parent) => tsWriter = parent;

            public static TouchstoneWriterCore Create(TouchstoneWriter parent)
            {
                return parent.Keywords.Version switch
                {
                    FileVersion.One => new TouchstoneWriterCoreV1(parent),
                    FileVersion.Two => new TouchstoneWriterCoreV2(parent),
                    _ => throw new NotImplementedException(),
                };
            }
        }
        class TouchstoneWriterCoreV1 : TouchstoneWriterCore
        {
            internal TouchstoneWriterCoreV1(TouchstoneWriter parent)
                : base(parent) { }

            private const int MaximumDataPairsPerLine = 4;

            public override int GetNumberOfDataPairsPerLine(int numPorts)
            {
                int pairs = base.GetNumberOfDataPairsPerLine(numPorts);
                if (pairs > MaximumDataPairsPerLine) pairs = MaximumDataPairsPerLine;
                return pairs;
            }

            public override void WriteHeader()
            {
                string options = FormatOptions(tsWriter.Options);
                tsWriter.Writer.WriteLine(options);
            }
            public override async Task WriteHeaderAsync()
            {
                string options = FormatOptions(tsWriter.Options);
                await tsWriter.Writer.WriteLineAsync(options);
            }

            public override ListFormat GetListFormat(int numPorts)
            {
                if (numPorts <= 2) return ListFormat.SourcePortMajor;
                else return ListFormat.DestinationPortMajor;
            }
        }
        class TouchstoneWriterCoreV2 : TouchstoneWriterCoreV1
        {
            internal TouchstoneWriterCoreV2(TouchstoneWriter parent)
                : base(parent) { }
            public override ListFormat GetListFormat(int numPorts)
            {
                if (numPorts == 2)
                {
                    TwoPortDataOrderConfig config = tsWriter.Keywords.TwoPortDataOrder ??
                                                                TwoPortDataOrderConfig.TwoOne_OneTwo;
                    return config switch
                    {
                        TwoPortDataOrderConfig.OneTwo_TwoOne => ListFormat.DestinationPortMajor,
                        TwoPortDataOrderConfig.TwoOne_OneTwo => ListFormat.SourcePortMajor,
                        _ => throw new NotImplementedException(),
                    };
                }
                else return ListFormat.DestinationPortMajor;
            }
            public override void WriteHeader()
            {
                throw new NotImplementedException();
                //base.WriteHeader();
                //tsWriter.Writer.WriteLine(ControlKeywords.NetworkData);
            }
        }
        #endregion
    }

}
