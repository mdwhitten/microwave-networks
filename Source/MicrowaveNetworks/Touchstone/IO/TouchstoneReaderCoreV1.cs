using System;
using System.Collections.Generic;
using MicrowaveNetworks.Internal;
using MicrowaveNetworks.Matrices;

namespace MicrowaveNetworks.Touchstone.IO
{
    public sealed partial class TouchstoneReader
    {
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

                    rawFlattenedMatrix.Count.IsPerfectSquare(out int numPorts);
                    ListFormat format = numPorts == 2 ? ListFormat.SourcePortMajor : ListFormat.DestinationPortMajor;                    

                    NetworkParametersMatrix matrix = tsReader.Options.Parameter switch
                    {
                        ParameterType.Scattering => new ScatteringParametersMatrix(parameters, format),
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
    }
}
