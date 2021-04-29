using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace MicrowaveNetworks.Touchstone.IO
{
    /// <summary>
    /// Defines additional formmatting options to use when creating the Touchstone file.
    /// </summary>
    public class TouchstoneWriterSettings
    {
        /// <summary>
        /// Specifies whether a comment line should be added above the network data indicating the parameter type, index, and unit.
        /// </summary>
        public bool IncludeColumnNames = true;
        /// <summary>
        /// Specifies the numeric format string that should be used for converting the double values to string when writing.
        /// </summary>
        public string NumericFormatString;
        /// <summary>
        /// Specifies the column width for the Touchstone file when <see cref="UnifiedColumnWidth"/> is true.
        /// </summary>
        public int ColumnWidth = 18;
        /// <summary>
        /// Specifies whether all data entries should be padded with spacing to ensure unified column widths when viewing the Touchstone file in a 
        /// text editor with constant width font. The column width is specified in <see cref="ColumnWidth"/>;
        /// </summary>
        public bool UnifiedColumnWidth = true;
        /// <summary>
        /// Specifies the numeric format provider to use when writing the Touchstone data.
        /// </summary>
        public IFormatProvider NumericFormatProvider = CultureInfo.CurrentCulture.NumberFormat;
    }
}
