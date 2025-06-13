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
	public class GeneralReaderTests
	{
		public static IEnumerable<object[]> HeaderMaps
			=> new object[][]
			{
                // Standard complete header
                new object[] {"# MHz S MA R 75", new TouchstoneOptionsLine
				{
					FrequencyUnit = TouchstoneFrequencyUnit.MHz,
					Parameter = ParameterType.Scattering,
					Resistance = 75,
					Reactance = 0,
					Format = TouchstoneDataFormat.MagnitudeAngle
				}},
                // Validates spacing with one or more whitespace characters
                new object[] {"#   MHz   S  MA R\t75", new TouchstoneOptionsLine
				{
					FrequencyUnit = TouchstoneFrequencyUnit.MHz,
					Parameter = ParameterType.Scattering,
					Resistance = 75,
					Format = TouchstoneDataFormat.MagnitudeAngle
				} },
                /*
                // Different ordering #1
                ("# R 50 Y Hz DB", new TouchstoneOptionsLine
                {
                    FrequencyUnit = TouchstoneFrequencyUnit.Hz,
                    Parameter = ParameterType.Admittance,
                    Resistance = 50,
                    Reactance = 0,
                    Format = TouchstoneDataFormat.DecibelAngle
                }),
                // Different ordering #2
                ("# G R 50 GHz RI", new TouchstoneOptionsLine
                {
                    FrequencyUnit = TouchstoneFrequencyUnit.GHz,
                    Parameter = ParameterType.HybridG,
                    Resistance = 50,
                    Reactance = 0,
                    Format = TouchstoneDataFormat.RealImaginary
                }),*/
                // Missing some values (valid per spec)
                new object[] {"# R 75", new TouchstoneOptionsLine
				{
					Resistance = 75,
					Reactance = 0,
				}},
                // Missing all values (valid per spec)
                new object[] {"#", new TouchstoneOptionsLine() },
                // Capitalization that deviates from standard capitalized values
                new object[] {"# hz s dB r 50", new TouchstoneOptionsLine
				{
					FrequencyUnit = TouchstoneFrequencyUnit.Hz,
					Parameter = ParameterType.Scattering,
					Resistance = 50,
					Reactance = 0,
					Format = TouchstoneDataFormat.DecibelAngle,
				} },
                // Standard complete header complex resistance
                new object[] {"# MHz S MA R (75-20j)", new TouchstoneOptionsLine
				{
					FrequencyUnit = TouchstoneFrequencyUnit.MHz,
					Parameter = ParameterType.Scattering,
					Resistance = 75,
					Reactance = -20,
					Format = TouchstoneDataFormat.MagnitudeAngle
				} },
                // Standard complete header complex resistance
                new object[] {"# MHz S MA R (75+20j)", new TouchstoneOptionsLine
				{
					FrequencyUnit = TouchstoneFrequencyUnit.MHz,
					Parameter = ParameterType.Scattering,
					Resistance = 75,
					Reactance = 20,
					Format = TouchstoneDataFormat.MagnitudeAngle
				} },
			};
		[TestMethod]
		[DynamicData(nameof(HeaderMaps))]
		public void TestHeaderParsing(string header, TouchstoneOptionsLine options)
		{
			var reader = OpenReaderFromText(header);
			reader.Options.Should().BeEquivalentTo(options);
			reader.Dispose();
		}
	}
}
