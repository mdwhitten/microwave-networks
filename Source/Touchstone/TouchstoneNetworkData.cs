using System;
using System.Collections.Generic;

using System.Threading.Tasks;
using System.IO;
using Touchstone.IO;

namespace Touchstone
{
    using ScatteringParameters;
    using System.Threading;

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
        Hz,
        kHz,
        MHz,
        GHz
    };
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
    public class TouchstoneNetworkData
    {
        public TouchstoneOptions Options { get; set; } = new TouchstoneOptions();
        public TouchstoneKeywords Keywords { get; set; } = new TouchstoneKeywords();

        public ScatteringParametersCollection ScatteringParameters { get; set; }

        internal TouchstoneNetworkData() { }
        public TouchstoneNetworkData(int numPorts, TouchstoneOptions opts)
        {
            ScatteringParameters = new ScatteringParametersCollection(numPorts);
        }

        private TouchstoneNetworkData(TouchstoneReader reader, CancellationToken token = default)
        {
            Options = reader.Options;
            Keywords = reader.Keywords;
            

            foreach (var (frequency, matrix) in reader)
            {
                if (ScatteringParameters == null)
                {
                    ScatteringParameters = new ScatteringParametersCollection(matrix.NumPorts);
                }
                ScatteringParameters.Add(frequency, matrix);
                token.ThrowIfCancellationRequested();
            }

            // If no version is set that makes this version 1.0
            Keywords.Version = Keywords.Version ?? FileVersion.One;
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

        public static TouchstoneNetworkData FromFile(string filePath, TouchstoneReaderSettings settings)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            using (TouchstoneReader reader = TouchstoneReader.CreateWithFile(filePath, settings))
            {
                return new TouchstoneNetworkData(reader);
            }
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

        public static TouchstoneNetworkData FromString(string fileText, TouchstoneReaderSettings settings)
        {
            if (fileText == null) throw new ArgumentNullException(nameof(fileText));

            using (TouchstoneReader reader = TouchstoneReader.CreateWithString(fileText, settings))
            {
                return new TouchstoneNetworkData(reader);
            }
        }
    }
}
