using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using MicrowaveNetworks.Matrices;
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
        class TouchstoneReaderCoreV1 : TouchstoneReaderCore
        {
            internal TouchstoneReaderCoreV1(TouchstoneReader reader) : base(reader) { }
            int? flattenedMatrixLength;
            readonly Queue<string> previewedLines = new Queue<string>();

            protected override void ReadHeader(string currentLine)
            {
                tsReader.ParseOption(currentLine);
            }
            public override FrequencyParametersPair? ReadNextMatrix()
            {
                List<string> rawFlattenedMatrix = new List<string>();
                FrequencyParametersPair? networkData = default;

                if (!flattenedMatrixLength.HasValue)
                {
                    if (!tsReader.MoveToNextValidLine())
                    {
                        tsReader.ThrowHelper("Data");
                    }
                    string firstLine = tsReader.ReadLineAndCount();
                    rawFlattenedMatrix.AddRange(TrimAndSplitLine(firstLine));

                    // We only need to perform this check if the network has 2 ports or more; a one port network only has a single
                    // data pair (i.e. two entries) plus frequency. We know that we don't need to investigate subsequent lines.
                    if (rawFlattenedMatrix.Count > 3)
                    {
                        while (tsReader.MoveToNextValidLine())
                        {
                            string line = tsReader.ReadLineAndCount();
                            var data = TrimAndSplitLine(line);
                            // Continued data lines split over multiple should always have an even number (pairs of complex data).
                            // New frequency points will have an odd number of values due to the frequency being present
                            if (data.Count % 2 == 0)
                            {
                                rawFlattenedMatrix.AddRange(data);
                            }
                            else
                            {
                                previewedLines.Enqueue(line);
                                break;
                            }
                        }
                    }
                    flattenedMatrixLength = rawFlattenedMatrix.Count;
                }
                else
                {
                    while (previewedLines.Count > 0 && rawFlattenedMatrix.Count < flattenedMatrixLength.Value)
                    {
                        string line = previewedLines.Dequeue();
                        rawFlattenedMatrix.AddRange(TrimAndSplitLine(line));
                    }
                    while (rawFlattenedMatrix.Count < flattenedMatrixLength.Value && tsReader.MoveToNextValidLine())
                    {
                        string line = tsReader.ReadLineAndCount();
                        rawFlattenedMatrix.AddRange(TrimAndSplitLine(line));
                    }
                }

                if (rawFlattenedMatrix.Count == flattenedMatrixLength.Value)
                {
                    var (frequency, parameters) = tsReader.ParseRawData(rawFlattenedMatrix);

                    NetworkParametersMatrix matrix = tsReader.Options.Parameter switch
                    {
                        ParameterType.Scattering => new ScatteringParametersMatrix(parameters, ListFormat.SourcePortMajor),
                        _ => throw new NotImplementedException($"Support for parameter type {tsReader.Options.Parameter} has not been implemented."),
                    };

                    networkData = new FrequencyParametersPair(frequency, matrix);
                }

                return networkData;
            }

            /*protected override Task<FrequencyParametersPair> ReadNextMatrixAsync()
            {
                throw new NotImplementedException();
            }*/
        }
        class TouchstoneReaderCoreV2 : TouchstoneReaderCore
        {
            private TouchstoneKeywordsSomethingEle Keywords { get; } = new TouchstoneKeywordsSomethingEle();

            internal TouchstoneReaderCoreV2(TouchstoneReader reader) : base(reader) { }

            static string[] allWhitespace = new[] { "\r\n", "\r", "\n", " " };

            private string ReadToNextKeyword(string initialValue = "")
            {
                StringBuilder sb = new StringBuilder(initialValue);

                int charVal;

                while ((charVal = tsReader.reader.Peek()) != -1)
                {
                    char character = (char)charVal;

                    if (character == '[')
                    {
                        break;
                    }
                    else
                    {
                        string line = tsReader.ReadLineAndCount();
                        sb.AppendLine(line);
                    }
                }
                return sb.ToString();
            }

            protected override void ReadHeader(string currentLine)
            {
                tsReader.ParseOption(currentLine);

                if (tsReader.MoveToNextValidLine())
                {
                    string portsKeyword = tsReader.ReadLineAndCount();
                    (TouchstoneKeywords keyword, string value) = tsReader.ParseKeyword(portsKeyword);
                    if (keyword != TouchstoneKeywords.NumberOfPorts)
                    {
                        tsReader.ThrowHelper("Header", "Number of ports keyword must follow immediately after the options line.");
                    }
                    if (!int.TryParse(value, out int ports)) tsReader.ThrowHelper("Header", "Invalid format specified for number of ports");
                    Keywords.NumberOfPorts = ports;
                }
                else
                {
                    tsReader.ThrowHelper("Header", "Incomplete file");
                }
                while (tsReader.MoveToNextValidLine())
                {
                    string line = tsReader.ReadLineAndCount();
                    (TouchstoneKeywords keyword, string value) = tsReader.ParseKeyword(line);

                    try
                    {
                        switch (keyword)
                        {
                            // We're at the start of the network data. Matrix parsing will happen from here
                            case TouchstoneKeywords.NetworkData:
                                return;
                            case TouchstoneKeywords.TwoPortOrder:
                                Keywords.TwoPortDataOrder = TouchstoneEnumMap<TwoPortDataOrderConfig>.FromTouchstoneValue(value);
                                break;
                            case TouchstoneKeywords.NumberOfFrequencies:
                                Keywords.NumberOfFrequencies = int.Parse(line);
                                break;
                            case TouchstoneKeywords.NumberOfNoiseFrequencies:
                                Keywords.NumberOfNoiseFrequencies = int.Parse(line);
                                break;
                            case TouchstoneKeywords.Reference:
                                string references = ReadToNextKeyword(value);
                                Keywords.Reference = references.Split(allWhitespace, StringSplitOptions.RemoveEmptyEntries).Select(r => float.Parse(r.Trim())).ToList();
                                break;
                            case TouchstoneKeywords.MatrixFormat:
                                Keywords.MatrixFormat = TouchstoneEnumMap<MatrixFormat>.FromTouchstoneValue(value);
                                break;
                            case TouchstoneKeywords.MixedModeOrder:
                                Keywords.MixedModeOrder = ReadToNextKeyword(value);
                                break;
                            case TouchstoneKeywords.BeginInformation:
                                string information = ReadToNextKeyword(value);
                                Keywords.Information = information;
                                break;
                            case TouchstoneKeywords.EndInformation:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        tsReader.ThrowHelper("Header", "Error parsing keywords", ex);
                    }
                }

            }

            public override FrequencyParametersPair? ReadNextMatrix()
            {
                List<string> rawFlattenedMatrix = new List<string>();
                int totalLength = default;

                if (Keywords.MatrixFormat is MatrixFormat.Lower || Keywords.MatrixFormat is MatrixFormat.Upper)
                {
                    // Sum of natural numbers to get total points
                    int sum = (int)((Keywords.NumberOfPorts) * (Keywords.NumberOfPorts + 1) / 2);
                    // Multiply for re/im or mag/phase and add one for frequency
                    totalLength = sum * 2 + 1;
                }
                else
                {
                    // Num ports squared times two (real/imag or mag/phase) plus one for the frequency
                    totalLength = (int)(Math.Pow((double)Keywords.NumberOfPorts, 2) * 2 + 1);
                }

                int rows = 0;
                do
                {
                    if (!tsReader.MoveToNextValidLine()) tsReader.ThrowHelper("Network Data", "Unexpected end of file reached");
                    string line = tsReader.ReadLineAndCount();
                    rawFlattenedMatrix.AddRange(TrimAndSplitLine(line));
                    rows++;
                }
                while (rawFlattenedMatrix.Count < totalLength);

                if (Keywords.MatrixFormat is MatrixFormat.Lower)
                {
                    for (int i = 1; i < Keywords.NumberOfPorts; i++)
                    {
                        // Have to multiply by 2 for re/im or mag/phase
                        int insertPoint = i * 2;
                        int numPoints = ((int)Keywords.NumberOfPorts - i) * 2;
                        rawFlattenedMatrix.InsertRange(insertPoint, Enumerable.Repeat("0", numPoints));
                    }
                }
                else if (Keywords.MatrixFormat is MatrixFormat.Upper)
                {
                    for (int i = 1; i < Keywords.NumberOfPorts; i++)
                    {
                        // Have to multiply by 2 for re/im or mag/phase
                        int insertPoint = (int)(i * Keywords.NumberOfPorts) * 2;
                        rawFlattenedMatrix.InsertRange(insertPoint, Enumerable.Repeat("0", i * 2));
                    }
                }

                var (frequency, parameters) = tsReader.ParseRawData(rawFlattenedMatrix);

                NetworkParametersMatrix matrix = tsReader.Options.Parameter switch
                {
                    ParameterType.Scattering => new ScatteringParametersMatrix(parameters, ListFormat.SourcePortMajor),
                    _ => throw new NotImplementedException($"Support for parameter type {tsReader.Options.Parameter} has not been implemented."),
                };

                if (Keywords.MatrixFormat is MatrixFormat.Upper || Keywords.MatrixFormat is MatrixFormat.Lower)
                {
                    matrix.TriangluarToSymmetric();
                }

                FrequencyParametersPair? networkData = new FrequencyParametersPair(frequency, matrix);

                return networkData;
 
            }

        }
        #endregion
    }
}
