using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MicrowaveNetworks.Touchstone.IO
{
    public class TouchstoneFileReader : TouchstoneReader 
    {
        internal TouchstoneFileReader(string filePath, TouchstoneReaderSettings settings)
            : base(settings)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            StreamReader reader = new StreamReader(filePath);
            ParseToData(reader);
        }
    }
}
