using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Touchstone.IO
{
    public sealed class TouchstoneStringWriter : TouchstoneWriter
    {

        public TouchstoneStringWriter(TouchstoneWriterSettings settings, TouchstoneOptions options)
            : base(settings, options)
        {
            StringWriter writer = new StringWriter();
            Writer = writer;
            //WriteHeader();
        }
        public TouchstoneStringWriter(TouchstoneWriterSettings settings)
            : base(settings)
        {
            StringWriter writer = new StringWriter();
            Writer = writer;
            //WriteHeader();
        }

        public override string ToString()
        {
            return Writer.ToString();
        }
    }
}
