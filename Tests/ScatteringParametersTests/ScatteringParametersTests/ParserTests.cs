using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Touchstone;
using Touchstone.IO;
using System.Linq;

namespace ScatteringParametersTests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            string filePath = @"C:\Users\mwhitten\Downloads\deembed\De-embed\15dB_Attenuator.s2p";

            TouchstoneReaderSettings settings = new TouchstoneReaderSettings
            {
                FrequencySelector = val => val > 4e9
            };

            TouchstoneNetworkData data = TouchstoneNetworkData.FromFile(filePath, settings);
            var stuff = data.ScatteringParameters.Select(d => (d.Frequency_Hz, d.Parameters[2, 1])).ToList();

        }
    }
}
