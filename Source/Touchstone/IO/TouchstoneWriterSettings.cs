using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Touchstone.IO
{
    public class TouchstoneWriterSettings
    {
        public bool IncludeColumnNames = true;
        public string NumericFormatString;
        public int ColumnWidth = 18;
        public bool UnifiedColumnWidth = true;
        public IFormatProvider NumericFormatProvider = CultureInfo.CurrentCulture.NumberFormat;
    }
}
