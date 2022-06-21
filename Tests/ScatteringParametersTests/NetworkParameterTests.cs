using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicrowaveNetworks;
using FluentAssertions;

namespace MicrowaveNetworksTests
{
    [TestClass]
    public class NetworkParameterTests
    {
        [TestMethod]
        public void OneTest()
        {
            NetworkParameter.One.Magnitude_dB.Should().Be(0);
            NetworkParameter.One.Phase_deg.Should().Be(0);
        }
        [TestMethod]
        public void ConversionTests()
        {
            NetworkParameter s = NetworkParameter.FromPolarDegree(100, 180);
            s.Magnitude_dB.Should().Be(40);
            s.Phase_deg.Should().Be(180);

            NetworkParameter s2 = NetworkParameter.FromPolarDecibelDegree(40, 180);
            s2.Magnitude.Should().Be(100);
            s.Phase_deg.Should().Be(180);

            NetworkParameter s3 = new NetworkParameter(10, 10);
            s3.Phase_deg.Should().Be(45);

            NetworkParameter s4 = new NetworkParameter(10, -10);
            s4.Phase_deg.Should().Be(-45);

            NetworkParameter s5 = NetworkParameter.FromPolarDegree(100 * Math.Sqrt(2), 45);
            s5.Real.Should().BeApproximately(100, .001);
            s5.Imaginary.Should().BeApproximately(100, .001);
        }
        [TestMethod]
        public void ComparisonTests()
        {
            NetworkParameter s = NetworkParameter.FromPolarDecibelDegree(20, 0);
            NetworkParameter s2 = NetworkParameter.FromPolarDecibelDegree(10, 180);

            //(s > s2).Should().BeTrue("magnitude is prioritized");

            NetworkParameter s_0 = NetworkParameter.FromPolarDecibelDegree(20, 10);
            //(s < s_0).Should().BeTrue("greater angle");

            NetworkParameter s3 = NetworkParameter.FromPolarDecibelDegree(40, 100);
            NetworkParameter s4 = NetworkParameter.FromPolarDegree(100, 100);
            (s3 == s4).Should().BeTrue();
        }
    }
}
