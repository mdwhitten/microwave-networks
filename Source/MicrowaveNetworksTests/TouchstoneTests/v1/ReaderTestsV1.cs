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
			ts.NetworkParameters.Count.Should().Be(5);

			ts.NetworkParameters[100e6, 1, 1].Should().Be(NetworkParameter.FromPolarDegree(0.99, -4));
			ts.NetworkParameters[300e6, 1, 1].Should().Be(NetworkParameter.FromPolarDegree(0.707, -45));
			ts.NetworkParameters[500e6, 1, 1].Should().Be(NetworkParameter.FromPolarDegree(0.01, -89));
		}

		[TestMethod]
		public void TestReadTwoPort()
		{
			Touchstone ts = FromText(SampleReaderFiles.TwoPort_v1);
			ts.NetworkParameters.NumberOfPorts.Should().Be(2);
			ts.NetworkParameters.Count.Should().Be(3);

			ts.NetworkParameters[1e9, 1, 2].Should().Be(new NetworkParameter(-0.0003, -0.0021));
			ts.NetworkParameters[2e9, 2, 1].Should().Be(new NetworkParameter(-0.0096, -0.0298));
			ts.NetworkParameters[10e9, 2, 2].Should().Be(new NetworkParameter(0.3420, 0.3337));
		}

		[TestMethod]
		public void TestReadFourPort()
		{
			Touchstone ts = FromText(SampleReaderFiles.FourPort_v1);
			ts.NetworkParameters.NumberOfPorts.Should().Be(4);
			ts.NetworkParameters.Count.Should().Be(3);

			ts.NetworkParameters[5e9, 1, 2].Should().Be(NetworkParameter.FromPolarDegree(0.40, -42.20));
			ts.NetworkParameters[5e9, 3, 3].Should().Be(NetworkParameter.FromPolarDegree(0.60, 161.24));
			ts.NetworkParameters[6e9, 4, 2].Should().Be(NetworkParameter.FromPolarDegree(0.41, -81.24));
			ts.NetworkParameters[7e9, 1, 2].Should().Be(NetworkParameter.FromPolarDegree(0.45, -46.41));
			ts.NetworkParameters[7e9, 4, 4].Should().Be(NetworkParameter.FromPolarDegree(0.50, 136.69));
		}
	}
}
