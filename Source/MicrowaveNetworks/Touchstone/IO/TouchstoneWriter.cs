using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MicrowaveNetworks.Touchstone.Internal;
using static MicrowaveNetworks.Internal.Utilities;
using static MicrowaveNetworks.Touchstone.Internal.Constants;

namespace MicrowaveNetworks.Touchstone.IO
{
    /// <summary>
    /// Provides lower-level support for rendering Touchstone files from network data and Touchstone options and keywords.
    /// </summary>
    public sealed partial class TouchstoneWriter : IDisposable
#if NET5_0_OR_GREATER
									, IAsyncDisposable
#endif
	{
		private TextWriter Writer { get; set; }
		private readonly Touchstone touchstone;

		readonly TouchstoneWriterSettings settings;
		readonly TouchstoneOptionsLine options;
		bool headerWritten;
		bool columnsWritten;
		TouchstoneWriterCore core;

		private static readonly FieldNameLookup<TouchstoneKeywords> keywordLookup = new FieldNameLookup<TouchstoneKeywords>();

		private TouchstoneWriter(TextWriter writer, Touchstone touchstone, TouchstoneWriterSettings settings)
		{
			this.settings = settings ?? new TouchstoneWriterSettings();
			if (!char.IsWhiteSpace(settings.ColumnSeparationChar))
			{
				throw new ArgumentException("The column separation character must be a whitespace character.");
			}
			Writer = writer ?? throw new ArgumentNullException(nameof(writer));

			options = new TouchstoneOptionsLine
			{
				Format = settings.DataFormat,
				FrequencyUnit = settings.FrequencyUnit,
				Parameter = touchstone.NetworkParameters.GetTouchstoneParameterType(),
				Resistance = touchstone.Resistance,
				Reactance = touchstone.Reactance
			};
			this.touchstone = touchstone;
		}

		#region Static Constructors
		/// <summary>
		/// Creates a new <see cref="TouchstoneWriter"/> using the specified file path with default <see cref="TouchstoneWriterSettings"/>.
		/// </summary>
		/// <param name="filePath">The file to which you want to write. The <see cref="TouchstoneWriter"/> creates a file at the specified path
		/// or overwrites the existing file.</param>
		/// <param name="touchstone">The Touchstone network data to write to the file.</param>
		/// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="filePath"/> value is null.</exception>
		public static TouchstoneWriter Create(string filePath, Touchstone touchstone) => Create(filePath, touchstone, new TouchstoneWriterSettings());
		/// <summary>
		/// Creates a new <see cref="TouchstoneWriter"/> using the specified file path with the specified settings.
		/// </summary>
		/// <param name="filePath">The file to which you want to write. The <see cref="TouchstoneWriter"/> creates a file at the specified path
		/// or overwrites the existing file.</param>
		/// <param name="settings">The <see cref="TouchstoneWriterSettings"/> used to configure the <see cref="TouchstoneWriter"/> instance. 
		/// If <paramref name="settings"/> is null the default settings will be used.</param>
		/// <param name="touchstone">The Touchstone network data to write to the file.</param>
		/// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
		public static TouchstoneWriter Create(string filePath, Touchstone touchstone, TouchstoneWriterSettings settings)
		{
			if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
			StreamWriter writer = new StreamWriter(filePath);
			return new TouchstoneWriter(writer, touchstone, settings);
		}
		/// <summary>
		/// Creates a new <see cref="TouchstoneWriter"/> using the specified <see cref="TextWriter"/> with default <see cref="TouchstoneWriterSettings"/>.
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter"/> to which you want to write. The Touchstone data will be appended to this <see cref="TextWriter"/>.</param>
		/// <param name="touchstone">The Touchstone network data to write to the file.</param>
		/// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="writer"/> value is null.</exception>
		public static TouchstoneWriter Create(TextWriter writer, Touchstone touchstone) => Create(writer, touchstone, new TouchstoneWriterSettings());
		/// <summary>
		/// Creates a new <see cref="TouchstoneWriter"/> using the specified <see cref="TextWriter"/> with default <see cref="TouchstoneWriterSettings"/>.
		/// </summary>
		/// <param name="writer">The <see cref="TextWriter"/> to which you want to write. The Touchstone data will be appended to this <see cref="TextWriter"/>.</param>
		/// <param name="settings">The <see cref="TouchstoneWriterSettings"/> used to configure the <see cref="TouchstoneWriter"/> instance. 
		/// If <paramref name="settings"/> is null the default settings will be used.</param>
		/// <param name="touchstone">The Touchstone network data to write to the file.</param>
		/// If the <see cref="TouchstoneParameterAttribute"/> "R" is complex it will be represented by its real part. 
		/// </param>
		/// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="writer"/> value is null.</exception>
		public static TouchstoneWriter Create(TextWriter writer, Touchstone touchstone, TouchstoneWriterSettings settings)
		{
			if (writer == null) throw new ArgumentNullException(nameof(writer));
			return new TouchstoneWriter(writer, touchstone, settings);
		}
		/// <summary>
		/// Creates a new <see cref="TouchstoneWriter"/> using the specified <see cref="StringBuilder"/> with default <see cref="TouchstoneWriterSettings"/>.
		/// </summary>
		/// <param name="sb">The <see cref="StringBuilder"/> to which you want to write. The Touchstone data will be appended to this <see cref="StringBuilder"/>.</param>
		/// <param name="touchstone">The Touchstone network data to write to the file.</param>
		/// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="sb"/> value is null.</exception>
		public static TouchstoneWriter Create(StringBuilder sb, Touchstone touchstone) => Create(sb, touchstone, new TouchstoneWriterSettings());
		/// <summary>
		/// Creates a new <see cref="TouchstoneWriter"/> using the specified <see cref="StringBuilder"/> with default <see cref="TouchstoneWriterSettings"/>.
		/// </summary>
		/// <param name="sb">The <see cref="StringBuilder"/> to which you want to write. The Touchstone data will be appended to this <see cref="StringBuilder"/>.</param>
		/// <param name="settings">The <see cref="TouchstoneWriterSettings"/> used to configure the <see cref="TouchstoneWriter"/> instance. 
		/// If the <see cref="TouchstoneParameterAttribute"/> "R" is complex it will be represented by its real part. 
		/// </param>
		/// If <paramref name="settings"/> is null the default settings will be used.</param>
		/// <param name="touchstone">The Touchstone network data to write to the file.</param>
		/// <returns>A new <see cref="TouchstoneWriter"/> object.</returns>
		/// <exception cref="ArgumentNullException">The <paramref name="sb"/> value is null.</exception>
		public static TouchstoneWriter Create(StringBuilder sb, Touchstone touchstone, TouchstoneWriterSettings settings)
		{
			if (sb == null) throw new ArgumentNullException(nameof(sb));
			StringWriter writer = new StringWriter(sb);
			return new TouchstoneWriter(writer, touchstone, settings);
		}
		#endregion
		#region Internal Formatting Functions
		private static string FormatOptions(TouchstoneOptionsLine options)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(OptionChar);

			string frequencyUnit = TouchstoneEnumMap<TouchstoneFrequencyUnit>.ToTouchstoneValue(options.FrequencyUnit);
			string parameter = TouchstoneEnumMap<ParameterType>.ToTouchstoneValue(options.Parameter);
			string format = TouchstoneEnumMap<TouchstoneDataFormat>.ToTouchstoneValue(options.Format);
			string resistance = "";
			if (options.Reactance != null && options.Reactance > 0)
			{
				string sign = options.Reactance >= 0 ? "+" : ""; // in case of negative number the "-" is already part of the Reactance value
				resistance = $"{ResistanceChar} ({options.Resistance:g}{sign}{options.Reactance:g}j)";
			}
			else
			{
				resistance = $"{ResistanceChar} {options.Resistance:g}";
			}

			return string.Join(" ", OptionChar, frequencyUnit, parameter, format, resistance);
		}
		//private static string FormatKeyword()
		private string FormatColumns(int numberOfPorts)
		{
			ListFormat format = core.GetListFormat(numberOfPorts);

			string frequencyDescription = $"Frequency ({settings.FrequencyUnit})";
			string columnPad = string.Empty;
			if (settings.UnifiedColumnWidth)
			{
				frequencyDescription = frequencyDescription.PadRight(settings.ColumnWidth - 2);
				columnPad = columnPad.PadRight(settings.ColumnWidth - 2);
			}

			//columns.Add(frequencyUnit);

			string parameter = TouchstoneEnumMap<ParameterType>.ToTouchstoneValue(options.Parameter);
			string description1 = null, description2 = null;
			switch (settings.DataFormat)
			{
				case TouchstoneDataFormat.DecibelAngle:
					description1 = "Mag (dB)";
					description2 = "Angle (deg)";
					break;
				case TouchstoneDataFormat.MagnitudeAngle:
					description1 = "Mag";
					description2 = "Angle (deg)";
					break;
				case TouchstoneDataFormat.RealImaginary:
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
				width = "," + settings.ColumnWidth;
				frequencyWidth = "," + (-settings.ColumnWidth);
				columnPad = string.Empty.PadRight(settings.ColumnWidth);
			}
			string compositeFormatString = $"{{0{width}:{formatString}}}";
			string frequencyCompositeString = $"{{0{frequencyWidth}:{formatString}}}";

			int numPorts = matrix.NumPorts;
			ListFormat format = core.GetListFormat(numPorts);

			List<double> parameters = new List<double>();

			// Prepare frequency
			double scaledFrequency = frequency / settings.FrequencyUnit.GetMultiplier();

			foreach (var (ports, parameter) in matrix.EnumerateParameters(format))
			{
				// Supports upper/lower matrix configuration for v2 files
				if (core.ShouldSkip(ports))
				{
					continue;
				}
				switch (settings.DataFormat)
				{
					case TouchstoneDataFormat.DecibelAngle:
						parameters.Add(parameter.Magnitude_dB);
						parameters.Add(parameter.Phase_deg);
						break;
					case TouchstoneDataFormat.MagnitudeAngle:
						parameters.Add(parameter.Magnitude);
						parameters.Add(parameter.Phase_deg);
						break;
					case TouchstoneDataFormat.RealImaginary:
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
				sb.Append(settings.ColumnSeparationChar);
				sb.Append(enumer.Current);
				enumer.MoveNext();
				sb.Append(settings.ColumnSeparationChar);
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
		/// <see cref="Keywords"/> is <see cref="TouchstoneFileVersion.Two"/>, the keywords will also be written to the file.</summary>
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
		/// <see cref="Keywords"/> is <see cref="TouchstoneFileVersion.Two"/>, the keywords will also be written to the file.</summary>
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

		public void WriteNetworkData()
		{
			if (!headerWritten) WriteHeader();
			core.BeginNetworkData();
			if (settings.IncludeColumnNames && !columnsWritten)
			{
				string columns = FormatColumns(touchstone.NetworkParameters.NumberOfPorts);
				WriteCommentLine(columns);
				columnsWritten = true;
			}
			foreach (var data in touchstone.NetworkParameters)
			{
				WriteData(data);
			}
		}
		public async Task WriteNetworkDataAsync(CancellationToken token = default)
		{
			if (!headerWritten) await WriteHeaderAsync();
			await core.BeginNetworkDataAsync();
			if (settings.IncludeColumnNames && !columnsWritten)
			{
				string columns = FormatColumns(touchstone.NetworkParameters.NumberOfPorts);
				await WriteCommentLineAsync(columns);
				columnsWritten = true;
			}
			foreach (var data in touchstone.NetworkParameters)
			{
				token.ThrowIfCancellationRequested();
				await WriteDataAsync(data);
			}
		}

		/// <summary>Writes the frequency and <see cref="NetworkParametersMatrix"/> contained in <paramref name="pair"/> to the network data of the file.</summary>
		/// <param name="pair">The <see cref="NetworkParametersMatrix"/> and corresponding frequency to write to the Touchstone file.</param>
		/// <remarks>If <see cref="WriteHeader"/> has not yet been called, this method will be called automatically before writing any network data.</remarks>
		private void WriteData(FrequencyParametersPair pair) => WriteData(pair.Frequency_Hz, pair.Parameters);
		/// <summary>Writes the frequency and <see cref="NetworkParametersMatrix"/> the network data of the file.</summary>
		/// <param name="frequency">The frequency of the network data to be written.</param>
		/// /// <param name="matrix">The network data to be written.</param>
		/// <remarks>If <see cref="WriteHeader"/> has not yet been called, this method will be called automatically before writing any network data.</remarks>
		private void WriteData(double frequency, NetworkParametersMatrix matrix)
		{
			string line = FormatEntry(frequency, matrix);
			Writer.WriteLine(line);
		}

		/// <summary>Asynchronously writes the frequency and <see cref="NetworkParametersMatrix"/> contained in <paramref name="pair"/> to the network data of the file.</summary>
		/// <param name="pair">The <see cref="NetworkParametersMatrix"/> and corresponding frequency to write to the Touchstone file.</param>
		/// <remarks>If <see cref="WriteHeaderAsync"/> has not yet been called, this method will be called automatically before writing any network data.</remarks>
		private async Task WriteDataAsync(FrequencyParametersPair pair) => await WriteDataAsync(pair.Frequency_Hz, pair.Parameters);
		/// <summary>Asynchronously writes the frequency and <see cref="NetworkParametersMatrix"/> the network data of the file.</summary>
		/// <param name="frequency">The frequency of the network data to be written.</param>
		/// /// <param name="matrix">The network data to be written.</param>
		/// <remarks>If <see cref="WriteHeaderAsync"/> has not yet been called, this method will be called automatically before writing any network data.</remarks>
		private async Task WriteDataAsync(double frequency, NetworkParametersMatrix matrix)
		{
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
					core.Dispose();
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
			// Specification requires an [End] keyword at the end of the file
			await core.DisposeAsync();
			if (Writer != null)
			{
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
	}

}
