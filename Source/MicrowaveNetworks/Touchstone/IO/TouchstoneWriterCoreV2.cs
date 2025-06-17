using MicrowaveNetworks.Touchstone.Internal;
using System;
using System.Linq;
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

			private void WriteKeywordBasic<T>(TouchstoneKeywords keyword, T value)
			{
				string formattedKeywordString = FormatKeywordBasic(keyword, value);
				tsWriter.Writer.WriteLine(formattedKeywordString);
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

			private static string FormatKeywordBasic<T>(TouchstoneKeywords keyword, T value) 
				=> FormatKeywordBasic(keyword, value?.ToString());
			private static string FormatKeywordBasic(TouchstoneKeywords keyword, string value)
			{
				string keywordTouchstone = TouchstoneEnumMap<TouchstoneKeywords>.ToTouchstoneValue(keyword);
				string formattedKeywordString = "";
				if (!string.IsNullOrEmpty(value))
				{
					formattedKeywordString = $"[{keywordTouchstone}] {value}";
				}
				else
				{
					formattedKeywordString = $"[{keywordTouchstone}]";
				}

				return formattedKeywordString;
			}

			public override void WriteHeader()
			{
				WriteKeywordBasic(TouchstoneKeywords.Version, "2.0");
				base.WriteHeader();
				WriteKeywordBasic(TouchstoneKeywords.NumberOfPorts, tsWriter.touchstone.NetworkParameters.NumberOfPorts);
				if (tsWriter.touchstone.NetworkParameters.NumberOfPorts == 2)
				{
					string dataOrder = TouchstoneEnumMap<TwoPortDataOrderConfig>.ToTouchstoneValue(tsWriter.settings.TwoPortDataOrder 
																											?? TwoPortDataOrderConfig.TwoOne_OneTwo);
					WriteKeywordBasic(TouchstoneKeywords.TwoPortDataOrder, dataOrder);
				}
				WriteKeywordBasic(TouchstoneKeywords.NumberOfFrequencies, tsWriter.touchstone.NetworkParameters.Count);
				if (tsWriter.touchstone.NoiseData?.Count > 0)
				{
					WriteKeywordBasic(TouchstoneKeywords.NumberOfNoiseFrequencies, tsWriter.touchstone.NoiseData.Count);
				}
				if (tsWriter.touchstone.Reference?.Count > 0)
				{
					var reference = string.Join(" ", tsWriter.touchstone.Reference);
					WriteKeywordBasic(TouchstoneKeywords.Reference, reference);
				}
				if (tsWriter.settings.MatrixFormat.HasValue)
				{
					string format = TouchstoneEnumMap<TouchstoneMatrixFormat>.ToTouchstoneValue(tsWriter.settings.MatrixFormat.Value);
					WriteKeywordBasic(TouchstoneKeywords.MatrixFormat, format);
				}
			}

			public override void BeginNetworkData()
			{
				WriteKeywordBasic(TouchstoneKeywords.NetworkData);
			}
			public override async Task BeginNetworkDataAsync()
			{
				await WriteKeywordBasicAsync(TouchstoneKeywords.NetworkData);
			}

			public override void Dispose()
			{
				WriteKeywordBasic(TouchstoneKeywords.End);
			}
			internal override bool ShouldSkip((int DestinationPort, int SourcePort) ports)
			{
				int numPorts = tsWriter.touchstone.NetworkParameters.NumberOfPorts;
				if (tsWriter.settings.MatrixFormat == TouchstoneMatrixFormat.Upper && numPorts > 1)
				{
					return (ports.DestinationPort > ports.SourcePort);
				}
				else if (tsWriter.settings.MatrixFormat == TouchstoneMatrixFormat.Lower && numPorts > 1)
				{
					return (ports.DestinationPort < ports.SourcePort);
				}
				else return false;
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
