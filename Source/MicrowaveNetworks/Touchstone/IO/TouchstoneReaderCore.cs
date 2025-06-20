using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using MicrowaveNetworks.Matrices;
using MicrowaveNetworks.Touchstone.IO;
using MicrowaveNetworks.Touchstone.Internal;

namespace MicrowaveNetworks.Touchstone.IO
{
    /// <summary>
    /// Provides lower-level support for reading Touchstone files from existing data sources.
    /// </summary>
    public sealed partial class TouchstoneReader : IDisposable
    {
        #region Core Reader Classes
        abstract class TouchstoneReaderCore
        {
            protected TouchstoneReader tsReader;

            protected TouchstoneReaderCore(TouchstoneReader reader)
            {
                this.tsReader = reader;
            }
            protected abstract void ReadHeader(string currentLine);

            public abstract FrequencyParametersPair? ReadNextMatrix();
            //protected abstract Task<(bool eof, FrequencyParametersPair matrix)> ReadNextMatrixAsync();

            public static TouchstoneReaderCore Create(TouchstoneReader tsReader)
            {
                TouchstoneReaderCore readerCore = null;
                string firstLine = default;

                if (tsReader.MoveToNextValidLine())
                {
                    firstLine = tsReader.ReadLineAndCount();
                }
                else tsReader.ThrowHelper("Header", "No valid information contained in file.");

                firstLine = firstLine.Trim();
                if (firstLine[0] == Constants.OptionChar)
                {
                    readerCore = new TouchstoneReaderCoreV1(tsReader);
                }
                else if (firstLine[0] == Constants.KeywordOpenChar)
                {

                    string versionTwo = TouchstoneEnumMap<TouchstoneFileVersion>.ToTouchstoneValue(TouchstoneFileVersion.Two);
                    if (tsReader.ParseKeyword(firstLine) == (TouchstoneKeywords.Version, versionTwo))
                    {
                        readerCore = new TouchstoneReaderCoreV2(tsReader);
                    }
                    else
                    {
                        tsReader.ThrowHelper("Invalid version specifier format");
                    }

                    // Now need to move to the next valid line to parse the header
                    firstLine = tsReader.ReadLineAndCount();
                }
                else
                {
                    tsReader.ThrowHelper("Header", "The Option Line (Touchstone format 1.0) or Version Keyword (Touchstone format 2.0) must be the first" +
                        "non-comment and non-blank line in the file.");
                }
                readerCore.ReadHeader(firstLine);
                return readerCore;
            }
        }
        #endregion
    }
}
