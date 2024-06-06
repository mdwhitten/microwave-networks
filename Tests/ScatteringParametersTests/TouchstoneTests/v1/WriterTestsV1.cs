﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using MicrowaveNetworks;
using MicrowaveNetworks.Touchstone;
using MicrowaveNetworks.Touchstone.IO;
using MicrowaveNetworks.Matrices;
using System.Linq;

namespace MicrowaveNetworksTests.TouchstoneTests
{
    [TestClass]
    public class WriterTestsV1
    {
        StringBuilder sb;
        TouchstoneWriter writer;

        public static IEnumerable<object[]> TestMatrices => new[]
        {
            new object[] { new ScatteringParametersMatrix(1) { [1,1] = new NetworkParameter(0.5, -0.5) } },
            new object[] { new ScatteringParametersMatrix(2)
            {
                [1,1] = NetworkParameter.One,
                [1, 2] = new NetworkParameter(0.9, -0.2),
                [2, 1] = new NetworkParameter(0.9, -0.2),
                [2, 2] = NetworkParameter.One
            }}
        };

        [TestInitialize]
        public void TestInit()
        {
            sb = new StringBuilder();
            writer = TouchstoneWriter.Create(sb, new TouchstoneWriterSettings
            {
                UnifiedColumnWidth = false,
                NumericFormatString = "g"
            });
        }

        [DataTestMethod]
        [DynamicData(nameof(TestMatrices))]
        public void TestMagnitudeAngleFormat(ScatteringParametersMatrix mtrx)
        {
            writer.Options = new TouchstoneOptions { Format = FormatType.MagnitudeAngle, FrequencyUnit = FrequencyUnit.Hz };
            double frequency = 1.0e9;
            // Write a dummy data line, flush, then clear the string builder. This way, we can look at just the line itself
            // and not have the header in the way
            writer.WriteData(frequency, mtrx);
            writer.Flush();
            sb.Clear();
            writer.WriteData(frequency, mtrx);
            // Line up the parameter components
            double[] parameters = mtrx.SelectMany(el => new[] { el.NetworkParameter.Magnitude, el.NetworkParameter.Phase_deg }).ToArray();
            string expected = FormatDataLine("g", frequency, parameters);

            string actual = sb.ToString().Trim();
            actual.Should().Be(expected);
        }

        [DataTestMethod]
        [DynamicData(nameof(TestMatrices))]
        public void TestRealImaginaryFormat(ScatteringParametersMatrix mtrx)
        {
            writer.Options = new TouchstoneOptions { Format = FormatType.RealImaginary, FrequencyUnit = FrequencyUnit.Hz };
            double frequency = 1.0e9;
            // Write a dummy data line, flush, then clear the string builder. This way, we can look at just the line itself
            // and not have the header in the way
            writer.WriteData(frequency, mtrx);
            writer.Flush();
            sb.Clear();
            writer.WriteData(frequency, mtrx);
            // Line up the parameter components
            double[] parameters  = mtrx.SelectMany(el => new[] { el.NetworkParameter.Real, el.NetworkParameter.Imaginary }).ToArray();
            string expected = FormatDataLine("g", frequency, parameters);
            
            string actual = sb.ToString().Trim();
            actual.Should().Be(expected);
        }

        [DataTestMethod]
        [DynamicData(nameof(TestMatrices))]
        public void TestDecibelsAngleFormat(ScatteringParametersMatrix mtrx)
        {
            writer.Options = new TouchstoneOptions { Format = FormatType.DecibelAngle, FrequencyUnit = FrequencyUnit.Hz };
            double frequency = 1.0e9;
            // Write a dummy data line, flush, then clear the string builder. This way, we can look at just the line itself
            // and not have the header in the way
            writer.WriteData(frequency, mtrx);
            writer.Flush();
            sb.Clear();
            writer.WriteData(frequency, mtrx);
            // Line up the parameter components
            double[] parameters = mtrx.SelectMany(el => new[] { el.NetworkParameter.Magnitude_dB, el.NetworkParameter.Phase_deg }).ToArray();
            string expected = FormatDataLine("g", frequency, parameters);

            string actual = sb.ToString().Trim();
            actual.Should().Be(expected);
        }

        private static string FormatDataLine(string numericFormat, double frequency, params double[] dataLines)
        {
            StringBuilder sb = new StringBuilder($"{{0:{numericFormat}}}");
            for (int i = 0; i < dataLines.Length; i++)
            {
                sb.Append($"\t{{{i + 1}:{numericFormat}}}");
            }

#if NET45
            var prepended = (new double[] { frequency }).Concat(dataLines);
            
            return string.Format(sb.ToString(), prepended.Cast<object>().ToArray());
#else
            return string.Format(sb.ToString(), dataLines.Prepend(frequency).Cast<object>().ToArray());
#endif
        }
    }
}
