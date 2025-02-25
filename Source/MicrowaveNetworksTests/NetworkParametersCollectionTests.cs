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
        static NetworkParametersCollection<ScatteringParametersMatrix> collection;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            collection = new NetworkParametersCollection<ScatteringParametersMatrix>(2);
            for (int i = 1; i <= 20; i++)
            {
                collection[i * 1.0e9, 2, 1] = NetworkParameter.FromPolarDecibelDegree(i, 0);
            }

        }

        [TestMethod]
        [DataRow(0, 1)]
        [DataRow(1.0e9, 1)]
        [DataRow(1.51e9, 2)]
        [DataRow(19.9e9, 20)]
        [DataRow(21e9, 20)]
        public void TestNearest(double testFrequency, double exptedMagnitude)
        {

            collection.Nearest(testFrequency)[2, 1].Should().Be(NetworkParameter.FromPolarDecibelDegree(exptedMagnitude, 0));

        }
    }
}
