using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Touchstone.ScatteringParameters;
using FluentAssertions;

namespace ScatteringParametersTests
{
    [TestClass]
    public class ScatteringParametersMatrixTests
    {
        [TestMethod]
        public void SparseTest()
        {
            ScatteringParametersMatrix sm = new ScatteringParametersMatrix(2)
            {
                [2, 1] = ScatteringParameter.FromMagnitudeDecibelAngle(10, 0)
            };


            sm[1, 1].Should().Be(ScatteringParameter.Unity);
            sm[1, 2].Should().Be(ScatteringParameter.Unity);
            sm[2, 2].Should().Be(ScatteringParameter.Unity);
        }
        [TestMethod]
        public void RangeTest()
        {
            ScatteringParametersMatrix sm = new ScatteringParametersMatrix(3);

            FluentActions.Invoking(() => sm[3, 0]).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => sm[0, 3]).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => sm[1, 4]).Should().Throw<ArgumentOutOfRangeException>();
            FluentActions.Invoking(() => sm[4, 1]).Should().Throw<ArgumentOutOfRangeException>();

            for (int i = 1; i <= 3; i++)
            {
                for (int j = 1; i <=3; i++)
                {
                    FluentActions.Invoking(() => sm[i, j]).Should().NotThrow();
                }
            }
        }
    }
}
