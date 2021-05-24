using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using MicrowaveNetworks.Touchstone;
using MicrowaveNetworks.Touchstone.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MicrowaveNetworks;
using System.Collections.Generic;
using MicrowaveNetworks.Matrices;

namespace MicrowaveNetworksTests.Touchstone
{

    [TestClass]
    public class ReaderTestsV1
    {
        static INetworkParametersCollection FromText(string text)
        {
            StringReader reader = new StringReader(text);
            using (TouchstoneReader tsReader = TouchstoneReader.Create(reader))
            {
                return tsReader.ReadToEnd();
            }
        }
        static TouchstoneReader OpenReaderFromText(string text)
        {
            StringReader reader = new StringReader(text);
            return TouchstoneReader.Create(reader);
        }

        [TestMethod]
        public void TestReadOnePort()
        {
            INetworkParametersCollection coll = default;
            FluentActions.Invoking(() => coll = FromText(SampleFiles.V1.OnePort)).Should().NotThrow();
            coll.NumberOfPorts.Should().Be(1);
            string tempPath = Path.GetTempFileName();
        }
        [TestMethod]
        public void TestReadFourPort()
        {
            INetworkParametersCollection coll = default;
            FluentActions.Invoking(() => coll = FromText(SampleFiles.V1.FourPort)).Should().NotThrow();
            coll.NumberOfPorts.Should().Be(4);
        }
        [TestMethod]
        public void TestHeaderParsing()
        {
            foreach ((string header, TouchstoneOptions options) in SampleFiles.V1.HeaderMaps)
            {
                var reader = OpenReaderFromText(header);
                reader.Options.Should().BeEquivalentTo(options);
                reader.Dispose();
            }
        }
    }
}
