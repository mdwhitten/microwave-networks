using MicrowaveNetworks.Matrices;
using MicrowaveNetworks.Touchstone.Internal;
using System;

namespace MicrowaveNetworks.Touchstone
{
    /// <summary>Specifies the unit of frequency in the Touchstone file.</summary>
    public enum TouchstoneFrequencyUnit
    {
        /// <summary>Specifies frequency units in Hz.</summary>
        Hz = 0,
        /// <summary>Specifies frequency units in kHz.</summary>
        kHz = 3,
        /// <summary>Specifies frequency units in MHz.</summary>
        MHz = 6,
        /// <summary>Specifies frequency units in GHz.</summary>
        GHz = 9
    };

    internal static class TouchstoneEnumExtensions
    {
        /// <summary>
        /// Returns the multiplier that corresponds with a given frequency unit.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        internal static double GetMultiplier(this TouchstoneFrequencyUnit unit) => Math.Pow(10, (int)unit);

        internal static ParameterType GetTouchstoneParameterType(this INetworkParametersCollection collection)
        {
            return collection switch
            {
                NetworkParametersCollection<ScatteringParametersMatrix> _ => ParameterType.Scattering,
                _ => throw new NotImplementedException()
            };
        }

        internal static ParameterType GetTouchstoneParameterType(this NetworkParametersMatrix collection)
        {
            return collection switch
            {
                ScatteringParametersMatrix _ => ParameterType.Scattering,
                _ => throw new NotImplementedException()
            };
        }
    }

    /// <summary>Represents the valid network parameter types as defined in the Touchstone specification.</summary>
    public enum ParameterType
    {
        /// <summary>Scattering parameters (S)</summary>
        [TouchstoneParameter("S")]
        Scattering,
        /// <summary>Admittance parameters (Y)</summary>
        [TouchstoneParameter("Y")]
        Admittance,
        /// <summary>Impedance parameters (Z)</summary>
        [TouchstoneParameter("Z")]
        Impedance,
        /// <summary>Hybrid-h parameters (H)</summary>
        [TouchstoneParameter("H")]
        HybridH,
        /// <summary>Hybrid-g parameters (G)</summary>
        [TouchstoneParameter("G")]
        HybridG
    }

    /// <summary>Represents the valid format types of the network parameter data pairs as defined in the Touchstone specification.</summary>
    public enum TouchstoneDataFormat
    {
        /// <summary>Decibel-angle (DB)</summary>
        [TouchstoneParameter("DB")]
        DecibelAngle,
        /// <summary>Magnitude-angle (MA)</summary>
        [TouchstoneParameter("MA")]
        MagnitudeAngle,
        /// <summary>Real-imaginary (RI)</summary>
        [TouchstoneParameter("RI")]
        RealImaginary
    }
    /// <summary>Represents valid version values for the Touchstone file as defined in the specification.</summary>
    public enum TouchstoneFileVersion
    {
        /// <summary>File format is based on the 1.0 specification.</summary>
        One,
        /// <summary>File format is based on the 2.0 specification.</summary>
        [TouchstoneParameter("2.0")]
        Two
    }
    /// <summary>Represents valid values for the <c>[Two-Port Data Order]</c> keyword, which is used to signify the column ordering convention 
    /// for 2-port network data.</summary>
    public enum TwoPortDataOrderConfig
    {
        /// <summary>Represents the keyword value <c>12_21</c>, indicating that N12 will precede N21.</summary>
        [TouchstoneParameter("12_21")]
        OneTwo_TwoOne,
        /// <summary>Represents the keyword value <c>21_12</c>, indicating that N21 will precede N12.</summary>
        [TouchstoneParameter("21_12")]
        TwoOne_OneTwo
    }
    /// <summary>
    /// Represents valid values for the <c>[Matrix Format]</c> keyword, which is used to define whether an entire matrix or a subset of all matrix 
    /// elements is given for single-ended data.</summary>
    public enum MatrixFormat
    {
        /// <summary>Indicates that the network data matrix contains all values.</summary>
        Full,
        /// <summary>Indicates that the network data matrix contains only the lower triangular part (including the diagonal).</summary>
        Lower,
        /// <summary>Indicates that the network data matrix contains only the upper triangular part (including the diagonal).</summary>
        Upper
    }
}
