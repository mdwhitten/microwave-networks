using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TouchstoneSnPFileReader.ScatteringParameters;
using FluentAssertions;

namespace ScatteringParametersTests
{
    [TestClass]
    public class ScatteringParameterTests
    {
        [TestMethod]
        public void UnityTest()
        {
            ScatteringParameter.Unity.Magnitude_dB.Should().Be(0);
            ScatteringParameter.Unity.Angle_deg.Should().Be(0);
        }
        [TestMethod]
        public void ConversionTests()
        {
            ScatteringParameter s = ScatteringParameter.FromMagnitudeAngle(100, 180);
            s.Magnitude_dB.Should().Be(20);
            s.Angle_deg.Should().Be(180);

            ScatteringParameter s2 = ScatteringParameter.FromMagnitudeDecibelAngle(20, 180);
            s2.Magnitude.Should().Be(100);
            s.Angle_deg.Should().Be(180);

            ScatteringParameter s3 = new ScatteringParameter(10, 10);
            s3.Angle_deg.Should().Be(45);

            ScatteringParameter s4 = new ScatteringParameter(10, -10);
            s4.Angle_deg.Should().Be(-45);

            ScatteringParameter s5 = ScatteringParameter.FromMagnitudeAngle(100 * Math.Sqrt(2), 45);
            s5.Real.Should().BeApproximately(100, .001);
            s5.Imaginary.Should().BeApproximately(100, .001);
        }
        [TestMethod]
        public void ComparisonTests()
        {
            ScatteringParameter s = ScatteringParameter.FromMagnitudeDecibelAngle(20, 0);
            ScatteringParameter s2 = ScatteringParameter.FromMagnitudeDecibelAngle(10, 180);

            (s > s2).Should().BeTrue("magnitude is prioritized");

            ScatteringParameter s_0 = ScatteringParameter.FromMagnitudeDecibelAngle(20, 10);
            (s < s_0).Should().BeTrue("greater angle");

            ScatteringParameter s3 = ScatteringParameter.FromMagnitudeDecibelAngle(20, 100);
            ScatteringParameter s4 = ScatteringParameter.FromMagnitudeAngle(100, 100);
            (s3 == s4).Should().BeTrue();
        }
    }
}
