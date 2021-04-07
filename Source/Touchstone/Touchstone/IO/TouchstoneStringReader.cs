using System;
using System.IO;

namespace MicrowaveNetworks.Touchstone.IO
{
    public class TouchstoneStringReader : TouchstoneReader 
    {
        internal TouchstoneStringReader(string fileText, TouchstoneReaderSettings settings)
            : base(settings)
        {
            if (string.IsNullOrEmpty(fileText)) throw new ArgumentNullException(nameof(fileText));

            StringReader reader = new StringReader(fileText);
            ParseToData(reader);
        }
    }
}
