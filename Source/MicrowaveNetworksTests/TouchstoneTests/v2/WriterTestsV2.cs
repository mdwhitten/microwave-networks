using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using MicrowaveNetworks;
using MicrowaveNetworks.Touchstone;
using MicrowaveNetworks.Touchstone.IO;
using MicrowaveNetworks.Matrices;
using System.Linq;
using static MicrowaveNetworksTests.TouchstoneTests.Utilities;
using System.IO;

namespace MicrowaveNetworksTests.TouchstoneTests
{
    [TestClass]
    public class WriterTestsV2
    {

		public static IEnumerable<object> Tests => new[]
		{
			new object[] { SampleWriterFiles.OnePort_v2, "#0.0000" },
			new object[] { SampleWriterFiles.TwoPort_v2_21_12, "##.##", TwoPortDataOrderConfig.TwoOne_OneTwo },
			new object[] { SampleWriterFiles.TwoPort_v2_12_21, "##.##", TwoPortDataOrderConfig.OneTwo_TwoOne },
			new object[] { SampleWriterFiles.FourPort_v2_FullMatrix, "##0.00", null, TouchstoneMatrixFormat.Full, '\t'}
		};

		[TestMethod]
		[DynamicData(nameof(Tests))]
		public void TestFileOutput(string fileData, string numericFormat, TwoPortDataOrderConfig? twoPortOrder = null, TouchstoneMatrixFormat? format = null, char? columnSeparationChar = ' ')
		{
			StringReader strReader = new StringReader(fileData);
			TouchstoneReader tsReader = TouchstoneReader.Create(strReader);

			var networkData = tsReader.ReadToEnd();
			var header = tsReader.Options;
			var resistance = tsReader.Resistance;

			Touchstone ts = FromText(fileData);

			var settings = new TouchstoneWriterSettings()
			{
				UnifiedColumnWidth = false,
				NumericFormatString = numericFormat,
				IncludeColumnNames = false,
				DataFormat = header.Format,
				FrequencyUnit = header.FrequencyUnit,
				ColumnWidth = 1,
				FileVersion = TouchstoneFileVersion.Two,
				ColumnSeparationChar = columnSeparationChar.GetValueOrDefault(' '),
				TwoPortDataOrder = twoPortOrder,
				MatrixFormat = format
			};

			ts.ToString(settings).Should().BeEquivalentTo(fileData);
		}
	}
}
