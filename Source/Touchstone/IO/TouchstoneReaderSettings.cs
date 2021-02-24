using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Touchstone.IO
{
    public sealed class TouchstoneReaderSettings
    {
        public bool ValidateFile;

        public Predicate<double> FrequencySelector;
        public Predicate<(int DestPort, int SourcePort)> ParameterSelector;

        public static TouchstoneReaderSettings Default => new TouchstoneReaderSettings
        {
            ValidateFile = true,
            FrequencySelector = null,
            ParameterSelector = null
        };
    }
}
