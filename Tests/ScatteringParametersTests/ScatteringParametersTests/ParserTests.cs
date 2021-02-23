using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TouchstoneSnPFileReader;
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

            TouchstoneFile file = TouchstoneFile.FromFile(filePath);

            var test = file.ScatteringParameters.Select(f => (f.Frequency_Hz, f.Parameters[2, 1]));
        }
    }
}
