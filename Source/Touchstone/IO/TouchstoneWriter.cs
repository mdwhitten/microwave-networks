using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Touchstone.ScatteringParameters;

namespace Touchstone.IO
{
    public abstract class TouchstoneWriter : IDisposable
    {
        public TouchstoneOptions Options { get; set; }
        public TouchstoneKeywords Keywords { get;  set; }

        TextWriter writer;
        TouchstoneWriterSettings settings;

        protected TouchstoneWriter(TouchstoneWriterSettings settings)
        {
            this.settings = settings;
        }
        public static string OptionsToString(TouchstoneOptions options)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Constants.OptionChar);

            string frequencyUnit = TouchstoneEnumMap<FrequencyUnit>.ToTouchstoneValue(options.FrequencyUnit);
            string parameter = TouchstoneEnumMap<ParameterType>.ToTouchstoneValue(options.Parameter);
            string format = TouchstoneEnumMap<FormatType>.ToTouchstoneValue(options.Format);
            string resistance = options.Resistance.ToString();

            return string.Join(" ", Constants.OptionChar, frequencyUnit, parameter, format, resistance);
        }
        protected void WriteHeader(TextWriter writer)
        {
            this.writer = writer;

            string options = OptionsToString(Options);
            writer.WriteLine(options);

            if (Keywords.Version.HasValue && Keywords.Version.Value == FileVersion.Two)
            {
                WriteKeywords();
            }
        }
        private void WriteKeywords()
        {
            throw new NotImplementedException();
        }
        private IEnumerable<string> FormatEntry(ScatteringParametersMatrix matrix)
        {
            throw new NotImplementedException();
        }

        public void WriteEntry(ScatteringParametersMatrix matrix)
        {
            foreach(string s in FormatEntry(matrix))
            {
                writer.WriteLine(s);
            }
        }
        public async Task WriteEntryAsync(ScatteringParametersMatrix matrix)
        {
            foreach (string s in FormatEntry(matrix))
            {
                await writer.WriteLineAsync(s);
            }
        }
        public void Flush() => writer.Flush();
        public async Task FlushAsync() => await writer.FlushAsync();

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writer?.Dispose();
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
