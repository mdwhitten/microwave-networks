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
        [TestMethod]
        public void SimpleRoundTripTest()
        {
            TouchstoneOptions options = new TouchstoneOptions
            {
                Format = FormatType.DecibelAngle,
                FrequencyUnit = FrequencyUnit.GHz,
                Parameter = ParameterType.Scattering,
                Resistance = 50
            };

            string filePath = Path.GetTempFileName();

            var ts = new Touchstone(2, options);
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
    }
}
