using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MicrowaveNetworks.Touchstone
{
	/// <summary>
	/// Describes the noise parameter data availabe in a Touchstone file.
	/// </summary>
    public class TouchstoneNoiseData
    {
		/// <summary>
		/// Specifies the minimum noise figure in decibels (dB).
		/// </summary>
		public double MinimumNoiseFigure { get; set; }

		/// <summary>
		/// Specifies the source reflection coefficient to realize minimum noise figure.
		/// </summary>
		public Complex SourceReflectionCoefficient { get; set; }

		/// <summary>
		/// Specifies the effective noise resistance.
		/// </summary>
		public double NoiseResistance { get; set; }
    }
}
