namespace MicrowaveNetworks.Touchstone.IO
{
	public sealed partial class TouchstoneWriter
    { 
		class TouchstoneWriterCoreV1 : TouchstoneWriterCore
        {
            internal TouchstoneWriterCoreV1(TouchstoneWriter parent)
                : base(parent) { }

            private const int MaximumDataPairsPerLine = 4;

            public override int GetNumberOfDataPairsPerLine(int numPorts)
            {
                int pairs = base.GetNumberOfDataPairsPerLine(numPorts);
                if (pairs > MaximumDataPairsPerLine) pairs = MaximumDataPairsPerLine;
                return pairs;
            }

            public override ListFormat GetListFormat(int numPorts)
            {
                if (numPorts <= 2) return ListFormat.SourcePortMajor;
                else return ListFormat.DestinationPortMajor;
            }
		}
    }

}
