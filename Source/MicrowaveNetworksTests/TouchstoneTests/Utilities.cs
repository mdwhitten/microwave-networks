using MicrowaveNetworks;
using MicrowaveNetworks.Touchstone;
using MicrowaveNetworks.Touchstone.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicrowaveNetworksTests.TouchstoneTests
{
	internal static class Utilities
	{
		internal static Touchstone FromText(string text)
		{
			StringReader reader = new StringReader(text);
			return new Touchstone(reader);
		}
		internal static TouchstoneReader OpenReaderFromText(string text)
		{
			StringReader reader = new StringReader(text);
			return TouchstoneReader.Create(reader);
		}
	}
}
