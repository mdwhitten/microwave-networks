using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;

namespace TouchstoneSnPFileReader
{
    using ScatteringParameters;
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
    public class TouchstoneFileOptions
    {
        public FrequencyUnit FrequencyUnit = FrequencyUnit.GHz;
        public ParameterType Parameter = ParameterType.Scattering;
        public FormatType Format = FormatType.MagnitudeAngle;
        [TouchstoneParameter("R")]
        public float Resistance = 50;
    }
    public class TouchstoneFileKeywords
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


        public TouchstoneFileOptions Options { get; set; } = new TouchstoneFileOptions();
        public TouchstoneFileKeywords Keywords { get; set; } = new TouchstoneFileKeywords();

        public ScatteringParametersCollection ScatteringParameters { get; set; }

        internal TouchstoneFile() { }
        public TouchstoneFile(int numPorts, TouchstoneFileOptions opts)
        {
            ScatteringParameters = new ScatteringParametersCollection(numPorts);
        }

        public static async Task<TouchstoneFile> FromFileAsync(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            using (StreamReader s = new StreamReader(filePath))
            {
                TouchstoneParser parser = new TouchstoneParser(s);
                return await parser.ParseAsync();
            }
        }

        public static TouchstoneFile FromFile(string filePath) => FromFileAsync(filePath).Result;

        public static async Task<TouchstoneFile> FromTextAsync(string fileText)
        {
            if (fileText == null) throw new ArgumentNullException(nameof(fileText));

            using (StringReader s = new StringReader(fileText))
            {
                TouchstoneParser parser = new TouchstoneParser(s);
                return await parser.ParseAsync();
            }
        }

        public static TouchstoneFile FromText(string fileText) => FromTextAsync(fileText).Result;
    }
}
