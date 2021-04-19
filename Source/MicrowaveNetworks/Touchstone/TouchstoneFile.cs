using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using MicrowaveNetworks.Touchstone.IO;
using MicrowaveNetworks.Matrices;
using MicrowaveNetworks.Internal;
using System.Threading.Tasks;

namespace MicrowaveNetworks.Touchstone
{
    internal sealed class TouchstoneParameterAttribute : Attribute
    {
        public string FieldName { get; }
        public TouchstoneParameterAttribute(string fieldName) => FieldName = fieldName;
    }

    /// <summary>
    /// Specifies the unit of frequency.
    /// </summary>
    public enum FrequencyUnit
    {
        Hz = 0,
        kHz = 3,
        MHz = 6,
        GHz = 9
    };
    public static class FrequencyUnitUtilities
    {
        /// <summary>
        /// Returns the multiplier that corresponds with a given frequency unit.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static double GetMultiplier(this FrequencyUnit unit) => Math.Pow(10, (int)unit);
    }

    /// <summary>
    /// Specifies what kind of network parameter data is contained in the file.
    /// </summary>
    public enum ParameterType
    {
        /// <summary>Scattering parameters (S)</summary>
        [TouchstoneParameter("S")]
        Scattering,
        /// <summary>Admittance parameters (Y)</summary>
        [TouchstoneParameter("Y")]
        Admittance,
        /// <summary>Impedance parameters (Z)</summary>
        [TouchstoneParameter("Z")]
        Impedance,
        /// <summary>Hybrid-h parameters (H)</summary>
        [TouchstoneParameter("H")]
        HybridH,
        /// <summary>Hybrid-g parameters (G)</summary>
        [TouchstoneParameter("G")]
        HybridG
    }
    /// <summary>
    /// Specifies the format of the network paramater data pairs.
    /// </summary>
    public enum FormatType
    {
        /// <summary>Decibel-angle (DB)</summary>
        [TouchstoneParameter("DB")]
        DecibelAngle,
        /// <summary>Magnitude-angle (MA)</summary>
        [TouchstoneParameter("MA")]
        MagnitudeAngle,
        /// <summary>Real-imaginary (RI)</summary>
        [TouchstoneParameter("RI")]
        RealImaginary
    }

    public enum FileVersion
    {
        One,
        [TouchstoneParameter("2.0")]
        Two
    }
    public enum TwoPortDataOrderConfig
    {
        [TouchstoneParameter("12_21")]
        OneTwo_TwoOne,
        [TouchstoneParameter("21_12")]
        TwoOne_OneTwo
    }

    public enum MatrixFormat
    {
        Full,
        Lower,
        Upper
    }
    public class TouchstoneOptions
    {
        public FrequencyUnit FrequencyUnit = FrequencyUnit.GHz;
        public ParameterType Parameter = ParameterType.Scattering;
        public FormatType Format = FormatType.MagnitudeAngle;
        [TouchstoneParameter("R")]
        public float Resistance = 50;
    }
    public class TouchstoneKeywords
    {
        [TouchstoneParameter("Version")]
        public FileVersion? Version;
        [TouchstoneParameter("Number of Ports")]
        public int? NumberOfPorts;
        [TouchstoneParameter("Two-Port Data Order")]
        public TwoPortDataOrderConfig? TwoPortDataOrder;
        [TouchstoneParameter("Number of Frequencies")]
        public int? NumberOfFrequencies;
        [TouchstoneParameter("Number of Noise Frequencies")]
        public int? NumberOfNoiseFrequencies;
        [TouchstoneParameter("Reference")]
        public List<float> Reference;
        [TouchstoneParameter("Matrix Format")]
        public MatrixFormat? MatrixFormat;
    }
    public class TouchstoneFile
    {
        public TouchstoneOptions Options { get; set; } = new TouchstoneOptions();
        public TouchstoneKeywords Keywords { get; set; } = new TouchstoneKeywords();

        public NetworkParametersCollection NetworkParameters { get; set; }

        internal TouchstoneFile() { }
        public TouchstoneFile(int numPorts, TouchstoneOptions opts)
        {
            Type parameterType = opts.Parameter.ToNetworkParameterMatrixType();
            NetworkParameters = new NetworkParametersCollection(numPorts, parameterType);
            Options = opts;
        }
        public TouchstoneFile(NetworkParametersCollection parameters)
        {
            NetworkParameters = parameters;
        }

        public TouchstoneFile(string filePath, TouchstoneReaderSettings settings)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            using (TouchstoneReader reader = TouchstoneReader.CreateWithFile(filePath, settings))
            {
                FromFile(reader);
            }
        }
        private void FromFile(TouchstoneReader reader, CancellationToken token = default)
        {
            Options = reader.Options;
            Keywords = reader.Keywords;

            NetworkParameters = ReadNetworkParameters(reader, token);

            // If no version is set that makes this version 1.0
            Keywords.Version = Keywords.Version ?? FileVersion.One;
        }

        private static NetworkParametersCollection ReadNetworkParameters(TouchstoneReader reader, CancellationToken token = default)
        {
            NetworkParametersCollection networkParameters = null;
            foreach (var (frequency, matrix) in reader)
            {
                if (networkParameters == null)
                {
                    Type paramType = reader.Options.Parameter.ToNetworkParameterMatrixType();
                    networkParameters = new NetworkParametersCollection(matrix.NumPorts, paramType);
                }
                networkParameters.Add(frequency, matrix);
                token.ThrowIfCancellationRequested();
            }
            return networkParameters;
        }
        public static NetworkParametersCollection ReadData(string filePath)
        {

            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            TouchstoneReaderSettings settings = new TouchstoneReaderSettings();

            using (TouchstoneReader reader = TouchstoneReader.CreateWithFile(filePath, settings))
            {
                return ReadNetworkParameters(reader);
            }
        }

        /*public static async Task<Touchstone> FromFileAsync(string filePath, TouchstoneReaderSettings settings)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            using (TouchstoneReader reader = TouchstoneReader.CreateWithFile(filePath, settings))
            {
                TouchstoneParser parser = new TouchstoneParser(s);
                return await parser.ParseAsync();
            }
        }*/
        public override string ToString()
        {
            TouchstoneStringWriter stringWriter = new TouchstoneStringWriter(new TouchstoneWriterSettings(), Options);

            foreach (var data in NetworkParameters)
            {
                stringWriter.WriteEntry(data);
            }
            return stringWriter.ToString();
        }
        public void Write(string filePath)
        {
            Write(filePath, new TouchstoneWriterSettings());
        }
        public void Write(string filePath, TouchstoneWriterSettings settings)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            using (TouchstoneFileWriter writer = new TouchstoneFileWriter(filePath, settings))
            {
                writer.Options = Options;
                writer.Keywords = Keywords;

                foreach (var pair in NetworkParameters)
                {
                    writer.WriteEntry(pair);
                }

                writer.Flush();
            };
        }
        public async Task WriteAsync(string filePath, CancellationToken token = default)
        {
            await WriteAsync(filePath, new TouchstoneWriterSettings());
        }
        public async Task WriteAsync(string filePath, TouchstoneWriterSettings settings, CancellationToken token = default)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            using (TouchstoneFileWriter writer = new TouchstoneFileWriter(filePath, settings))
            {
                writer.Options = Options;
                writer.Keywords = Keywords;
                writer.CancelToken = token;

                foreach (var pair in NetworkParameters)
                {
                    token.ThrowIfCancellationRequested();
                    await writer.WriteEntryAsync(pair);
                }

                writer.Flush();
            };
        }
        /*public static async Task<Touchstone> FromTextAsync(string fileText)
        {
            if (fileText == null) throw new ArgumentNullException(nameof(fileText));

            using (StringReader s = new StringReader(fileText))
            {
                TouchstoneParser parser = new TouchstoneParser(s);
                return await parser.ParseAsync();
            }
        }*/
        /*
        public static TouchstoneFile FromString(string fileText, TouchstoneReaderSettings settings)
        {
            if (fileText == null) throw new ArgumentNullException(nameof(fileText));

            using (TouchstoneReader reader = TouchstoneReader.CreateWithString(fileText, settings))
            {
                return new TouchstoneFile(reader);
            }
        }*/
    }
}
