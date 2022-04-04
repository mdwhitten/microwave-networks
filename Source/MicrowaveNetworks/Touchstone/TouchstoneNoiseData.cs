using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MicrowaveNetworks.Touchstone
{
    public class TouchstoneNoiseData
    {
        public double MinimumNoiseFigure { get; set; }

        public Complex SourceReflectionCoefficient { get; set; }

        public double NoiseResistance { get; set; }
    }
}
