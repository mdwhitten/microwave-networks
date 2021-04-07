using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Touchstone.IO
{
    class TouchstoneFileWriter : TouchstoneWriter
    {
        public TouchstoneFileWriter(string filePath, TouchstoneWriterSettings settings, TouchstoneOptions options)
       : base(settings, options)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            StreamWriter writer = new StreamWriter(filePath);
            Writer = writer;
        }
        public TouchstoneFileWriter(string filePath, TouchstoneWriterSettings settings)
            : base(settings)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            StreamWriter writer = new StreamWriter(filePath);
            Writer = writer;
        }
    }
}
