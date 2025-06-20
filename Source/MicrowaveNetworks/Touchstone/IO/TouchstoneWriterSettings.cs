using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using MicrowaveNetworks.Touchstone.Internal;

namespace MicrowaveNetworks.Touchstone.IO
{
    /// <summary>
    /// Defines additional formmatting options to use when creating the Touchstone file.
    /// </summary>
    public class TouchstoneWriterSettings
    {
        /// <summary>Specifies the file version format to use when creating the Touchstone file. See the specification for more information.</summary>
        [TouchstoneKeyword("Version")]
        public TouchstoneFileVersion FileVersion { get; set; } = TouchstoneFileVersion.One;

        /// <summary>Specifies the unit of frequency in the file.</summary>
        public TouchstoneFrequencyUnit FrequencyUnit { get; set; } = TouchstoneFrequencyUnit.GHz;

        /// <summary>Specifies the format of the network parameter data pairs in the file.</summary>
        public TouchstoneDataFormat DataFormat { get; set; } = TouchstoneDataFormat.MagnitudeAngle;

        /// <summary>Signifies the column ordering convention for two-port network data when <see cref="Version"/>
        /// is <see cref="TouchstoneFileVersion.Two"/>.</summary>
        public TwoPortDataOrderConfig? TwoPortDataOrder { get; set; } = TwoPortDataOrderConfig.TwoOne_OneTwo;

		/// <summary>
		/// Specifies the value of the <c>[Matrix Format]</c> keyword, which is used to define whether an entire matrix or a subset of all matrix 
		/// elements is given for single-ended data.</summary>
		public TouchstoneMatrixFormat? MatrixFormat { get; set; } = TouchstoneMatrixFormat.Full;

        /// <summary>
        /// Specifies whether a comment line should be added above the network data indicating the parameter type, index, and unit.
        /// </summary>
        public bool IncludeColumnNames { get; set; } = true;

        /// <summary>
        /// Specifies the numeric format string that should be used for converting the double values to string when writing.
        /// </summary>
        public string NumericFormatString { get; set; }

        /// <summary>
        /// Specifies the column width for the Touchstone file when <see cref="UnifiedColumnWidth"/> is true.
        /// </summary>
        public int ColumnWidth { get; set; } = 18;

        /// <summary>
        /// Specifies whether all data entries should be padded with spacing to ensure unified column widths when viewing the Touchstone file in a 
        /// text editor with constant width font. The column width is specified in <see cref="ColumnWidth"/>;
        /// </summary>
        public bool UnifiedColumnWidth { get; set; } = true;

        /// <summary>
        /// Specifies the numeric format provider to use when writing the Touchstone data.
        /// </summary>
        public IFormatProvider NumericFormatProvider { get; set; } = CultureInfo.CurrentCulture.NumberFormat;
    }
}
