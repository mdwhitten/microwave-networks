using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using MicrowaveNetworks.Touchstone;
using MicrowaveNetworks.Touchstone.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MicrowaveNetworks;
using MicrowaveNetworks.Matrices;


namespace MicrowaveNetworksTests.TouchstoneTests
{
    [TestClass]
    public class TouchstoneFileTests
    {
        static TouchstoneReader OpenReaderFromText(string text)
        {
            StringReader reader = new StringReader(text);
            return TouchstoneReader.Create(reader);
        }

        [TestMethod]
        public void SimpleRoundTripTest()
        {
            string filePath = Path.GetTempFileName();

            var ts = new Touchstone(2);
            int gain_dB = 0;
            for (double f = 1e9; f <= 2e9; f += 0.1e9)
            {
                var gain = NetworkParameter.FromPolarDecibelDegree(gain_dB, 0);
                ts.NetworkParameters[f, 2, 1] = gain;
            }

            ts.Write(filePath);

            var result = Touchstone.ReadAllData(filePath);

            result.Count.Should().Be(ts.NetworkParameters.Count);

            foreach (var freq in result.Frequencies)
            {
                ts.NetworkParameters[freq, 2, 1].Should().Be(result[freq, 2, 1]);
            }
        }
        /*[TestMethod]
        public void DetailedOptionsRoundTripTest()
        {
            TouchstoneOptionsLine defaultOptions = new TouchstoneOptionsLine
            {
                Format = TouchstoneDataFormat.DecibelAngle,
                FrequencyUnit = TouchstoneFrequencyUnit.GHz,
                Parameter = ParameterType.Scattering,
                Resistance = 50
            };

            string filePath = Path.GetTempFileName();

            var ts = new Touchstone(2);
            int gain_dB = 0;
            for (double f = 1e9; f <= 2e9; f += 0.1e9)
            {
                var gain = NetworkParameter.FromPolarDecibelDegree(gain_dB, 0);
                ts.NetworkParameters[f, 2, 1] = gain;
            }

            foreach ((string header, TouchstoneOptionsLine options) in TestCases.V1.HeaderMaps)
            {
                var reader = OpenReaderFromText(header);
                if (reader.Options.Parameter == ParameterType.Scattering)
                {
                    ts.Resistance = reader.Options.Resistance;
                    ts.Reactance = reader.Options.Reactance;
                    reader.Options.Should().BeEquivalentTo(options);
                    reader.Dispose();

                    ts.Write(filePath, new TouchstoneWriterSettings
                    {
                        DataFormat = options.Format,
                        FrequencyUnit = options.FrequencyUnit
                    });

                    var touchstoneData = new Touchstone(filePath);

                    using (TouchstoneReader tsReader = TouchstoneReader.Create(filePath))
                    {
                        tsReader.Read();
                        tsReader.Options.Should().BeEquivalentTo(options);
                    }

                }
                
            }
        }*/

    }
}
