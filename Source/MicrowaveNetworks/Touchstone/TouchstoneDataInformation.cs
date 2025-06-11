using System;
using System.Collections.Generic;
using System.Text;

namespace MicrowaveNetworks.Touchstone
{
    public class TouchstoneDataInformation
    {
        public ParameterType Type { get; set; } = ParameterType.Scattering;
        public float Resistance { get; set; } = 50.0f;
        public int NumberOfPorts { get; set; } = 2;

    }
}
