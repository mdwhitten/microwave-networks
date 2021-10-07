using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicrowaveNetworks;
using MicrowaveNetworks.Matrices;
using FluentAssertions;

namespace MicrowaveNetworksTests
{
    [TestClass]
    public class NetworkParametersCollectionTests
    {
        [TestMethod]
        public void TestNearest()
        {
            var collection = new NetworkParametersCollection<ScatteringParametersMatrix>(2);
            for (int i = 1; i <= 20; i++)
            {
                collection[i * 1.0e9, 2, 1] = NetworkParameter.FromPolarDecibelDegree(i, 0);
            }

            collection.Nearest(0)[2, 1].Should().Be(NetworkParameter.FromPolarDecibelDegree(1, 0), 
                "0 is out of bounds but closest to 1e9");

            collection.Nearest(1.0e9)[2, 1].Should().Be(NetworkParameter.FromPolarDecibelDegree(1, 0), 
                "1e9 is equal to 1e9");

            collection.Nearest(1.51e9)[2, 1].Should().Be(NetworkParameter.FromPolarDecibelDegree(2, 0),
                "1.51e9 is closer to 2e9");

            collection.Nearest(19.9e9)[2, 1].Should().Be(NetworkParameter.FromPolarDecibelDegree(20, 0),
                "19.9e9 is nearest to 20e9");

            collection.Nearest(21e9)[2, 1].Should().Be(NetworkParameter.FromPolarDecibelDegree(20, 0),
                "21e9 is outside of bounds but nearest to 20e9");

        }
    }
}
