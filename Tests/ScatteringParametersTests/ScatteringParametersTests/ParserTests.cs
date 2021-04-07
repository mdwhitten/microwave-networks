using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using MicrowaveNetworks.Touchstone;
using MicrowaveNetworks.Touchstone.IO;
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
                //FrequencySelector = val => val > 4e9
            };

            TouchstoneNetworkData data = TouchstoneNetworkData.FromFile(filePath, settings);
            var stuff = data.NetworkParameters.Select(d => (d.Frequency_Hz, d.Parameters[2, 1])).ToList();

            using (TouchstoneStringWriter writer = new TouchstoneStringWriter(new TouchstoneWriterSettings(),
                data.Options))
            {
                writer.WriteHeader();

                foreach (var val in data.NetworkParameters) writer.WriteEntry(val);

                string test = writer.ToString();
            }
            string tempPath = Path.GetTempFileName();
            data.ToFile(tempPath, new TouchstoneWriterSettings());
        }
    }
}
