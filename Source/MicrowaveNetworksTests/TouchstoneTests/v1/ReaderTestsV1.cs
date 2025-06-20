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
using static MicrowaveNetworksTests.TouchstoneTests.Utilities;

namespace MicrowaveNetworksTests.TouchstoneTests
{

    [TestClass]
    public class ReaderTestsV1
    {
        [TestMethod]
        public void TestReadOnePort()
        {
            Touchstone ts = FromText(SampleReaderFiles.OnePort_v1);
            ts.NetworkParameters.NumberOfPorts.Should().Be(1);
        }
        [TestMethod]
        public void TestReadFourPort()
        {
			Touchstone ts = FromText(SampleReaderFiles.FourPort_v1);
			ts.NetworkParameters.NumberOfPorts.Should().Be(4);
		}
    }
}
