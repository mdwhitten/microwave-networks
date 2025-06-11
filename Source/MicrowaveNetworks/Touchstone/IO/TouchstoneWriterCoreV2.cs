using MicrowaveNetworks.Touchstone.Internal;
using System;
using System.Threading.Tasks;

namespace MicrowaveNetworks.Touchstone.IO
{
	public sealed partial class TouchstoneWriter
	{
		class TouchstoneWriterCoreV2 : TouchstoneWriterCoreV1
		{
			internal TouchstoneWriterCoreV2(TouchstoneWriter parent)
				: base(parent) { }

			public override ListFormat GetListFormat(int numPorts)
			{
				if (numPorts == 2)
				{
					TwoPortDataOrderConfig config = tsWriter.settings.TwoPortDataOrder ??
																TwoPortDataOrderConfig.TwoOne_OneTwo;
					return config switch
					{
						TwoPortDataOrderConfig.OneTwo_TwoOne => ListFormat.DestinationPortMajor,
						TwoPortDataOrderConfig.TwoOne_OneTwo => ListFormat.SourcePortMajor,
						_ => throw new NotImplementedException(),
					};
				}
				else return ListFormat.DestinationPortMajor;
			}
			private void WriteKeywordBasic(TouchstoneKeywords keyword, string value = null)
			{
				string formattedKeywordString = FormatKeywordBasic(keyword, value);
				tsWriter.Writer.WriteLine(formattedKeywordString);
			}
			private async Task WriteKeywordBasicAsync(TouchstoneKeywords keyword, string value = null)
			{
				string formattedKeywordString = FormatKeywordBasic(keyword, value);
				await tsWriter.Writer.WriteLineAsync(formattedKeywordString);
			}

			private static string FormatKeywordBasic(TouchstoneKeywords keyword, string value)
			{
				string keywordTouchstone = TouchstoneEnumMap<TouchstoneKeywords>.ToTouchstoneValue(keyword);
				string formattedKeywordString = "";
				if (!string.IsNullOrEmpty(value))
				{
					formattedKeywordString = $"[{keyword}] {value}";
				}
				else
				{
					formattedKeywordString = $"[{keyword}]";
				}

				return formattedKeywordString;
			}

			public override void WriteHeader()
			{
				WriteKeywordBasic(TouchstoneKeywords.Version, "2.0");
				base.WriteHeader();
				WriteKeywordBasic(TouchstoneKeywords.NumberOfPorts, tsWriter.touchstone.NetworkParameters.NumberOfPorts.ToString());
			}

			public override void BeginNetworkData()
			{
				WriteKeywordBasic(TouchstoneKeywords.NumberOfFrequencies, tsWriter.touchstone.NetworkParameters.Count.ToString());
				if (tsWriter.touchstone.NetworkParameters.NumberOfPorts == 2)
				{
					string dataOrder = TouchstoneEnumMap<TwoPortDataOrderConfig>.ToTouchstoneValue(tsWriter.settings.TwoPortDataOrder 
																											?? TwoPortDataOrderConfig.TwoOne_OneTwo);
					WriteKeywordBasic(TouchstoneKeywords.TwoPortOrder, dataOrder);
				}
				WriteKeywordBasic(TouchstoneKeywords.NetworkData);
			}
			public override async Task BeginNetworkDataAsync()
			{
				await WriteKeywordBasicAsync(TouchstoneKeywords.NumberOfFrequencies, tsWriter.touchstone.NetworkParameters.Count.ToString());
				if (tsWriter.touchstone.NetworkParameters.NumberOfPorts == 2)
				{
					string dataOrder = TouchstoneEnumMap<TwoPortDataOrderConfig>.ToTouchstoneValue(tsWriter.settings.TwoPortDataOrder
																											?? TwoPortDataOrderConfig.TwoOne_OneTwo);
					await WriteKeywordBasicAsync(TouchstoneKeywords.TwoPortOrder, dataOrder);
				}
				await WriteKeywordBasicAsync(TouchstoneKeywords.NetworkData);
			}

			public override void Dispose()
			{
				WriteKeywordBasic(TouchstoneKeywords.End);
			}
#if NET5_0_OR_GREATER
			public override async ValueTask DisposeAsync()
			{
				if (tsWriter.Writer != null)
				{
					await WriteKeywordBasicAsync(TouchstoneKeywords.End);
				}
				else await ValueTask.CompletedTask;
			}
#endif
		}
	}

}
