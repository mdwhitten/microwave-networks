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
        public bool IncludeColumnNames = true;
        public string NumericFormatString;
        public int ColumnWidth = 18;
        public bool UnifiedColumnWidth = true;
        public IFormatProvider NumericFormatProvider = CultureInfo.CurrentCulture.NumberFormat;
    }
}
