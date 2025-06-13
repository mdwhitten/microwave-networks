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
    public class ReaderTestsV2
    {


        [TestMethod]
        public void TestReadOnePort()
        {
            Touchstone touchstone = null;
            FluentActions.Invoking(() => touchstone = FromText(SampleReaderFiles.OnePort_v2)).Should().NotThrow();
			touchstone.NetworkParameters.NumberOfPorts.Should().Be(1);
			touchstone.Reference.Should().Equal(new List<float> { 20 });
		}

        [TestMethod]
        public void TestReadTwoPort_21_12_Format()
        {
            Touchstone touchstone = FromText(SampleReaderFiles.TwoPort_v2_21_12);
            touchstone.NetworkParameters.NumberOfPorts.Should().Be(2);

            var expectedAt2Ghz = NetworkParameter.FromPolarDegree(3.57, 157);
            var expectedAt22Ghz = NetworkParameter.FromPolarDegree(1.30, 40);

            touchstone.NetworkParameters[2.0e9][2, 1].Should().Be(expectedAt2Ghz);
			touchstone.NetworkParameters[22.0e9][2, 1].Should().Be(expectedAt22Ghz);
			touchstone.Reference.Should().Equal(new List<float> { 50, 25 });
		}

		[TestMethod]
		public void TestReadTwoPort_12_21_Format()
		{
			Touchstone touchstone = FromText(SampleReaderFiles.TwoPort_v2_12_21);
			touchstone.NetworkParameters.NumberOfPorts.Should().Be(2);

			var expectedAt2Ghz = NetworkParameter.FromPolarDegree(3.57, 157);
			var expectedAt22Ghz = NetworkParameter.FromPolarDegree(1.30, 40);

			touchstone.NetworkParameters[2.0e9][2, 1].Should().Be(expectedAt2Ghz);
			touchstone.NetworkParameters[22.0e9][2, 1].Should().Be(expectedAt22Ghz);
            touchstone.Reference.Should().Equal(new List<float> { 50, 25 });
		}
		
		[TestMethod]
		public void TestReadTwoPortMissingDataOrder()
		{
            FluentActions.Invoking(() => FromText(SampleReaderFiles.TwoPort_v2_No_Order)).Should().Throw<InvalidDataException>();
		}
		[TestMethod]
		public void TestReadFourPort_FullMatrix_Format()
		{
			Touchstone touchstone = FromText(SampleReaderFiles.FourPort_v2_FullMatrix);
			touchstone.NetworkParameters.NumberOfPorts.Should().Be(4);
			touchstone.Reference.Should().Equal(new List<float> { 50, 75, 0.01f, 0.01f });

			var s11 = NetworkParameter.FromPolarDegree(.6, 161.24);
			var s13 = NetworkParameter.FromPolarDegree(0.42, -66.58);
			var s22 = NetworkParameter.FromPolarDegree(.6, 161.2);
			var s34 = NetworkParameter.FromPolarDegree(0.4, -42.2);
			var s42 = NetworkParameter.FromPolarDegree(0.42, -66.58);

			foreach(double freq in new List<double> { 5e9, 6e9, 7e9 })
			{
				touchstone.NetworkParameters[freq][1, 1].Should().Be(s11);
				touchstone.NetworkParameters[freq][1, 3].Should().Be(s13);
				touchstone.NetworkParameters[freq][2, 2].Should().Be(s22);
				touchstone.NetworkParameters[freq][3, 4].Should().Be(s34);
				touchstone.NetworkParameters[freq][4, 2].Should().Be(s42);
			}
		}

		[TestMethod]
		public void TestReadFourPort_LowerMatrix_Format()
		{
			Touchstone touchstone = FromText(SampleReaderFiles.FourPort_v2_LowerMatrix);
			touchstone.NetworkParameters.NumberOfPorts.Should().Be(4);
			touchstone.Reference.Should().Equal(new List<float> { 50, 75, 0.01f, 0.01f });

			for (int i = 1; i <=4; i++)
			{
				for (int j = 1; j <=4; j++)
				{
					var param = touchstone.NetworkParameters[5.0e9][i, j];
					var inverse = touchstone.NetworkParameters[5.0e9][j, i];
					param.Should().Be(inverse);
				}
			}


			var s12 = NetworkParameter.FromPolarDegree(0.4, -42.2);
			var s13 = NetworkParameter.FromPolarDegree(0.42, -66.58);
			var s14 = NetworkParameter.FromPolarDegree(0.53, -79.34);
			var s23 = NetworkParameter.FromPolarDegree(.53, -79.34);
			var s24 = NetworkParameter.FromPolarDegree(0.42, -66.58);
			var s34 = NetworkParameter.FromPolarDegree(0.40, -42.20);

			touchstone.NetworkParameters[5.0e9][1, 2].Should().Be(s12);
			touchstone.NetworkParameters[5.0e9][1, 3].Should().Be(s13);
			touchstone.NetworkParameters[5.0e9][1, 4].Should().Be(s14);
			touchstone.NetworkParameters[5.0e9][2, 3].Should().Be(s23);
			touchstone.NetworkParameters[5.0e9][2, 4].Should().Be(s24);
			touchstone.NetworkParameters[5.0e9][3, 4].Should().Be(s34);
		}

		[TestMethod]
		public void TestReadFourPort_UpperMatrix_Format()
		{
			Touchstone touchstone = FromText(SampleReaderFiles.FourPort_v2_UpperMatrix);
			touchstone.NetworkParameters.NumberOfPorts.Should().Be(4);
			touchstone.Reference.Should().Equal(new List<float> { 50, 75, 0.01f, 0.01f });

			for (int i = 1; i <= 4; i++)
			{
				for (int j = 1; j <= 4; j++)
				{
					var param = touchstone.NetworkParameters[5.0e9][i, j];
					var inverse = touchstone.NetworkParameters[5.0e9][j, i];
					param.Should().Be(inverse);
				}
			}

			var s21 = NetworkParameter.FromPolarDegree(0.4, -42.2);
			var s31 = NetworkParameter.FromPolarDegree(0.42, -66.58);
			var s32 = NetworkParameter.FromPolarDegree(.53, -79.34);
			var s41 = NetworkParameter.FromPolarDegree(0.53, -79.34);
			var s42 = NetworkParameter.FromPolarDegree(0.42, -66.58);
			var s43 = NetworkParameter.FromPolarDegree(0.40, -42.20);

			touchstone.NetworkParameters[5.0e9][2, 1].Should().Be(s21);
			touchstone.NetworkParameters[5.0e9][3, 1].Should().Be(s31);
			touchstone.NetworkParameters[5.0e9][3, 2].Should().Be(s32);
			touchstone.NetworkParameters[5.0e9][4, 1].Should().Be(s41);
			touchstone.NetworkParameters[5.0e9][4, 2].Should().Be(s42);
			touchstone.NetworkParameters[5.0e9][4, 3].Should().Be(s43);
		}
	}
}
