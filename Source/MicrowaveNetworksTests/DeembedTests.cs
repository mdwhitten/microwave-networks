using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicrowaveNetworks;
using MicrowaveNetworks.Matrices;
using FluentAssertions;
using FluentAssertions.Equivalency;

namespace MicrowaveNetworksTests
{
	[TestClass]
	public class DeembedTests
	{
		[TestMethod]
		public void SimpleDeembed()
		{
			NetworkParameter infiniteReturnLoss = NetworkParameter.FromPolarDecibelDegree(double.NegativeInfinity, 0);
			NetworkParameter threedBLoss = NetworkParameter.FromPolarDecibelDegree(-3, 0);
			NetworkParameter onedBLoss = NetworkParameter.FromPolarDecibelDegree(-1, 0);
			NetworkParameter fivedBLoss = NetworkParameter.FromPolarDecibelDegree(-5, 0);

			ScatteringParametersMatrix leadIn = new ScatteringParametersMatrix(2)
			{
				[1, 1] = infiniteReturnLoss,
				[1, 2] = threedBLoss,
				[2, 1] = threedBLoss,
				[2, 2] = infiniteReturnLoss
			};
			ScatteringParametersMatrix dut = new ScatteringParametersMatrix(2)
			{
				[1, 1] = infiniteReturnLoss,
				[1, 2] = onedBLoss,
				[2, 1] = onedBLoss,
				[2, 2] = infiniteReturnLoss
			};
			ScatteringParametersMatrix dutMeasured = new ScatteringParametersMatrix(2)
			{
				[1, 1] = infiniteReturnLoss,
				[1, 2] = onedBLoss * threedBLoss * fivedBLoss,
				[2, 1] = onedBLoss * threedBLoss * fivedBLoss,
				[2, 2] = infiniteReturnLoss
			};


			ScatteringParametersMatrix leadOut = new ScatteringParametersMatrix(2)
			{
				[1, 1] = infiniteReturnLoss,
				[1, 2] = fivedBLoss,
				[2, 1] = fivedBLoss,
				[2, 2] = infiniteReturnLoss
			};

			ScatteringParametersMatrix result = dutMeasured.Deembed(leadIn, leadOut);

			result[2, 1].Magnitude_dB.Should().BeApproximately(dut[2, 1].Magnitude_dB, 1e-6);
			result[2, 1].Phase_deg.Should().BeApproximately(dut[2, 1].Phase_deg, 1e-6);
		}

		static NetworkParameter RandomNetworkParameter()
		{
			Random rand = new Random();
			double magnitude = rand.NextDouble() * 10 - 5; // Random value between -5 and 5 dB
			double phase = rand.NextDouble() * 360; // Random phase between 0 and 360 degrees
			return NetworkParameter.FromPolarDecibelDegree(magnitude, phase);
		}

		static ScatteringParametersMatrix Random()
		{
			ScatteringParametersMatrix matrix = new ScatteringParametersMatrix(2);
			for (int i = 1; i <= 2; i++)
			{
				for (int j = 1; j <= 2; j++)
				{
					matrix[i, j] = RandomNetworkParameter();
				}
			}
			return matrix;
		}


		void NetworkParamCompare(IAssertionContext<PortNetworkParameterPair> compare)
		{
			compare.Subject.NetworkParameter.Real.Should().BeApproximately(compare.Expectation.NetworkParameter.Real, 1e-6);
			compare.Subject.NetworkParameter.Imaginary.Should().BeApproximately(compare.Expectation.NetworkParameter.Imaginary, 1e-6);
		}
		EquivalencyOptions<PortNetworkParameterPair> PortNetworkParamOptions(EquivalencyOptions<PortNetworkParameterPair> options)
		{
			options
				.WithStrictOrdering()
				.Using<PortNetworkParameterPair>(NetworkParamCompare)
				.WhenTypeIs<PortNetworkParameterPair>();
			return options;
		}

		[TestMethod]
		public void CascadeDeembedRoundTrip()
		{
			var leadIn = Random();
			var dut = Random();
			var leadOut = Random();


			var measured = NetworkParametersMatrix.Cascade(leadIn, dut, leadOut);
			var duMeasuredLeadIn = NetworkParametersMatrix.Cascade(leadIn, dut);
			var duMeasuredLeadOut = NetworkParametersMatrix.Cascade(dut, leadOut);

			var dutPlusLeadIn = measured.DeembedRight(leadOut);
			var dutPlusLeadOut = measured.DeembedLeft(leadIn);
			var dutDeembed = measured.Deembed(leadIn, leadOut);//Deembed(leadOut);

			dutPlusLeadIn.Should().BeEquivalentTo(duMeasuredLeadIn, PortNetworkParamOptions);
			dutPlusLeadOut.Should().BeEquivalentTo(duMeasuredLeadOut, PortNetworkParamOptions);	
			dutDeembed.Should().BeEquivalentTo(dut, PortNetworkParamOptions);
		}
	}
}
