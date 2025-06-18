using System;
using System.Threading.Tasks;

namespace MicrowaveNetworks.Touchstone.IO
{
	public sealed partial class TouchstoneWriter
	{
		abstract class TouchstoneWriterCore : IDisposable
#if NET5_0_OR_GREATER
									, IAsyncDisposable
#endif
		{
			protected TouchstoneWriter tsWriter;

			public virtual int GetNumberOfDataPairsPerLine(int numPorts)
			{
				if (numPorts <= 2)
				{
					return 4;
				}
				else return numPorts;
			}

			public abstract ListFormat GetListFormat(int numPorts);

			public virtual void WriteHeader()
			{
				string options = FormatOptions(tsWriter.options);
				tsWriter.Writer.WriteLine(options);
			}

			public virtual async Task WriteHeaderAsync()
			{
				string options = FormatOptions(tsWriter.options);
				await tsWriter.Writer.WriteLineAsync(options);
			}
			public virtual void BeginNetworkData() { }
			public virtual async Task BeginNetworkDataAsync() { await Task.FromResult(""); }

			protected TouchstoneWriterCore(TouchstoneWriter parent) => tsWriter = parent;

			public static TouchstoneWriterCore Create(TouchstoneWriter parent)
			{
				return parent.settings.FileVersion switch
				{
					TouchstoneFileVersion.One => new TouchstoneWriterCoreV1(parent),
					TouchstoneFileVersion.Two => new TouchstoneWriterCoreV2(parent),
					_ => throw new NotImplementedException(),
				};
			}
			internal virtual bool ShouldSkip((int DestinationPort, int SourcePort) ports) => false;

			public virtual void Dispose() { }
#if NET5_0_OR_GREATER
			public async virtual ValueTask DisposeAsync() { await ValueTask.CompletedTask; }

#endif
		}
	}

}
