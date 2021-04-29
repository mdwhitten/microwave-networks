using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using MicrowaveNetworks.Touchstone;
using MicrowaveNetworks.Touchstone.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MicrowaveNetworks;

namespace ScatteringParametersTests
{
    public static class SampleFIles
    {
        public static class V1
        {
            public const string OnePort =
@"# MHz S MA R 75
!freq magZ11 angZ11
100 0.99 -4
200 0.80 -22
300 0.707 -45
400 0.40 -62
500 0.01 -89";
            public const string FourPort =
@"! 4-port S-parameter data, taken at three frequency points
! note that data points need not be aligned
# GHz S MA R 50
5.00000 0.60 161.24 0.40 -42.20 0.42 -66.58 0.53 -79.34 !row 1
        0.40 -42.20 0.60 161.20 0.53 -79.34 0.42 -66.58 !row 2
        0.42 -66.58 0.53 -79.34 0.60 161.24 0.40 -42.20 !row 3
        0.53 -79.34 0.42 -66.58 0.40 -42.20 0.60 161.24 !row 4
6.00000 0.57 150.37 0.40 -44.34 0.41 -81.24 0.57 -95.77 !row 1
        0.40 -44.34 0.57 150.37 0.57 -95.77 0.41 -81.24 !row 2
        0.41 -81.24 0.57 -95.77 0.57 150.37 0.40 -44.34 !row 3
        0.57 -95.77 0.41 -81.24 0.40 -44.34 0.57 150.37 !row 4
7.00000 0.50 136.69 0.45 -46.41 0.37 -99.09 0.62 -114.19 !row 1
0.45 -46.41 0.50 136.69 0.62 -114.19 0.37 -99.09 !row 2
0.37 -99.09 0.62 -114.19 0.50 136.69 0.45 -46.41 !row 3
0.62 -114.19 0.37 -99.09 0.45 -46.41 0.50 136.69 !row 4";
        }
    }


    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            string filePath = @"C:\Users\mwhitten\OneDrive - NI\Case Documents\pSemi\VNA Eval\S2P Files\Coupler Path.s2p";

            var data = new TouchstoneFile(filePath);

            var test2 = TouchstoneFile.ReadData(filePath).Where(pair => pair.Frequency_Hz > 2e9).ToNetworkParametersCollection();

            StringBuilder sb = new StringBuilder();
            using (TouchstoneWriter writer = TouchstoneWriter.Create(sb))
            {
                writer.Options = data.Options;
                writer.WriteHeader();

                foreach (var val in data.NetworkParameters) writer.WriteData(val);

                string test = sb.ToString();
            }
            string tempPath = Path.GetTempFileName();
            data.Write(tempPath);
        }
    }
    [TestClass]
    public class ReaderTestsV1
    {
        static NetworkParametersCollection FromText(string text)
        {
            StringReader reader = new StringReader(text);
            using (TouchstoneReader tsReader = TouchstoneReader.Create(reader))
            {
                return tsReader.ReadToEnd();
            }
        }

        [TestMethod]
        public void TestReadOnePort()
        {
            NetworkParametersCollection coll = default;
            FluentActions.Invoking(() => coll = FromText(SampleFIles.V1.OnePort)).Should().NotThrow();
            coll.NumberOfPorts.Should().Be(1);
            string tempPath = Path.GetTempFileName();
        }
        [TestMethod]
        public void TestReadFourPort()
        {
            NetworkParametersCollection coll = default;
            FluentActions.Invoking(() => coll = FromText(SampleFIles.V1.FourPort)).Should().NotThrow();
            coll.NumberOfPorts.Should().Be(4);
        }
    }
}
