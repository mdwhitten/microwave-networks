using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicrowaveNetworks.Touchstone.IO
{
    public sealed class TouchstoneReaderSettings
    {
        public bool ValidateFile = true;

        public Predicate<double> FrequencySelector = null;
        public Predicate<(int DestPort, int SourcePort)> ParameterSelector = null;
    }
}
