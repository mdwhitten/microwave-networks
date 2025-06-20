using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicrowaveNetworks;
using MicrowaveNetworks.Matrices;
using FluentAssertions;

namespace MicrowaveNetworksTests
{
    [TestClass]
    public class CascadeTests
    {
        [TestMethod]
        public void SimpleCascade()
        {
            NetworkParameter infiniteReturnLoss = NetworkParameter.FromPolarDecibelDegree(double.NegativeInfinity, 0);
            NetworkParameter three_dBLoss = NetworkParameter.FromPolarDecibelDegree(3, 0);
            NetworkParameter five_dBLoss = NetworkParameter.FromPolarDecibelDegree(5, 0);

            ScatteringParametersMatrix s1 = new ScatteringParametersMatrix(2)
            {
                [1, 1] = infiniteReturnLoss,
                [1, 2] = three_dBLoss,
                [2, 1] = three_dBLoss,
                [2, 2] = infiniteReturnLoss
            };
            ScatteringParametersMatrix s2 = new ScatteringParametersMatrix(2)
            {
                [1, 1] = infiniteReturnLoss,
                [1, 2] = five_dBLoss,
                [2, 1] = five_dBLoss,
                [2, 2] = infiniteReturnLoss
            };

            ScatteringParametersMatrix result = NetworkParametersMatrix.Cascade(s1, s2);

            result[2, 1].Magnitude_dB.Should().BeApproximately(8, 1e-6);
            result[2, 1].Phase_deg.Should().BeApproximately(0, 1e-6);
        }
    }
}
