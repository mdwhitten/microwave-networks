using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MicrowaveNetworks.Matrices;
using MicrowaveNetworks.Touchstone.Internal;

namespace MicrowaveNetworks.Touchstone.IO
{
    public sealed partial class TouchstoneReader
    {
        class TouchstoneReaderCoreV2 : TouchstoneReaderCore
        {
            public TouchstoneFileVersion Version;
            public int? NumberOfPorts;
            public TwoPortDataOrderConfig? TwoPortDataOrder;
            public int? NumberOfFrequencies;
            public int? NumberOfNoiseFrequencies;
            public MatrixFormat? Format;
            public string MixedModeOrder;

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
                    NumberOfPorts = ports;
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
                                TwoPortDataOrder = TouchstoneEnumMap<TwoPortDataOrderConfig>.FromTouchstoneValue(value);
                                break;
                            case TouchstoneKeywords.NumberOfFrequencies:
                                NumberOfFrequencies = int.Parse(line);
                                break;
                            case TouchstoneKeywords.NumberOfNoiseFrequencies:
                                NumberOfNoiseFrequencies = int.Parse(line);
                                tsReader.NoiseData = new Dictionary<double, TouchstoneNoiseData>();
                                break;
                            case TouchstoneKeywords.Reference:
                                string references = ReadToNextKeyword(value);
                                tsReader.Reference = references.Split(allWhitespace, StringSplitOptions.RemoveEmptyEntries).Select(r => float.Parse(r.Trim())).ToList();
                                break;
                            case TouchstoneKeywords.MatrixFormat:
                                Format = TouchstoneEnumMap<MatrixFormat>.FromTouchstoneValue(value);
                                break;
                            case TouchstoneKeywords.MixedModeOrder:
                                MixedModeOrder = ReadToNextKeyword(value);
                                break;
                            case TouchstoneKeywords.BeginInformation:
                                string information = ReadToNextKeyword(value);
                                tsReader.AdditionalInformation = information;
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

                if (Format is MatrixFormat.Lower || Format is MatrixFormat.Upper)
                {
                    // Sum of natural numbers to get total points
                    int sum = (int)((NumberOfPorts) * (NumberOfPorts + 1) / 2);
                    // Multiply for re/im or mag/phase and add one for frequency
                    totalLength = sum * 2 + 1;
                }
                else
                {
                    // Num ports squared times two (real/imag or mag/phase) plus one for the frequency
                    totalLength = (int)(Math.Pow((double)NumberOfPorts, 2) * 2 + 1);
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

                if (Format is MatrixFormat.Lower)
                {
                    for (int i = 1; i < NumberOfPorts; i++)
                    {
                        // Have to multiply by 2 for re/im or mag/phase
                        int insertPoint = i * 2;
                        int numPoints = ((int)NumberOfPorts - i) * 2;
                        rawFlattenedMatrix.InsertRange(insertPoint, Enumerable.Repeat("0", numPoints));
                    }
                }
                else if (Format is MatrixFormat.Upper)
                {
                    for (int i = 1; i < NumberOfPorts; i++)
                    {
                        // Have to multiply by 2 for re/im or mag/phase
                        int insertPoint = (int)(i * NumberOfPorts) * 2;
                        rawFlattenedMatrix.InsertRange(insertPoint, Enumerable.Repeat("0", i * 2));
                    }
                }

                var (frequency, parameters) = tsReader.ParseRawData(rawFlattenedMatrix);

                NetworkParametersMatrix matrix = tsReader.Options.Parameter switch
                {
                    ParameterType.Scattering => new ScatteringParametersMatrix(parameters, ListFormat.SourcePortMajor),
                    _ => throw new NotImplementedException($"Support for parameter type {tsReader.Options.Parameter} has not been implemented."),
                };

                if (Format is MatrixFormat.Upper || Format is MatrixFormat.Lower)
                {
                    matrix.TriangluarToSymmetric();
                }

                FrequencyParametersPair? networkData = new FrequencyParametersPair(frequency, matrix);

                return networkData;
 
            }

        }
    }
}
