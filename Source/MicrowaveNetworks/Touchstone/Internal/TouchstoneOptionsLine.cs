namespace MicrowaveNetworks.Touchstone.Internal
{
    /// <summary>
    /// Represents the information contained in the option line of the Touchstone file based on the specification.
    /// </summary>
    /// <remarks>This object will be created when a Touchstone file is parsed, and the values of the fields will be used to create the option line when 
    /// a file is exported. See the specification at https://ibis.org/touchstone_ver2.0/touchstone_ver2_0.pdf for more information.</remarks>
    internal class TouchstoneOptions
    {
        /// <summary>Specifies the unit of frequency in the file.</summary>
        public TouchstoneFrequencyUnit FrequencyUnit = TouchstoneFrequencyUnit.GHz;
        /// <summary>Specifies what kind of network parameter data is contained in the file.</summary>
        public ParameterType Parameter = ParameterType.Scattering;
        /// <summary>Specifies the format of the network paramater data pairs in the file.</summary>
        public TouchstoneDataFormat Format = TouchstoneDataFormat.MagnitudeAngle;
        /// <summary>Specifies the reference resistance in ohms, where <see cref="Resistance"/> is a real, positive number of ohms.</summary>
        [TouchstoneParameter("R")]
        public float Resistance = 50;

        /// <summary>
        /// Returns a new <see cref="TouchstoneOptions"/> with default values according to the specification.
        /// </summary>
        public static TouchstoneOptions Default = new TouchstoneOptions();
    }
}
