using System;
using System.Numerics;
using static System.Math;

namespace MicrowaveNetworks
{
    /// <summary>
    /// A simple wrapper around <see cref="Complex"/> adding support for creating/reading the magnitude value in 
    /// decibels and phase value in degrees. Implicit conversions exist to/from <see cref="Complex"/>, as well as
    /// a public constructor to create from an existing object. Some functions of <see cref="Complex"/>
    /// hav been exposed here, but for those that are not exposed convert to/from a <see cref="Complex"/> to access them.
    /// </summary>
    public readonly struct NetworkParameter : IEquatable<Complex>, IEquatable<NetworkParameter>
    {
        private readonly Complex complex;

        /// <summary>Gets the imaginary component of the current object.</summary>
        public double Imaginary => complex.Imaginary;
        /// <summary>Gets the real component of the current of the current object.</summary>
        public double Real => complex.Real;
        /// <summary>Gets the magnitude (or absolute value) of a complex number.</summary>
        public double Magnitude => complex.Magnitude;
        /// <summary>Gets the magnitude (or absolute value) of a complex number in decibels (dB).</summary>
        public double Magnitude_dB => complex.Magnitude.FromLinear();
        /// <summary>Gets the phase of a complex number in radians.</summary>
        public double Phase => complex.Phase;
        /// <summary>Gets the phase of a complex number in degrees.</summary>
        public double Phase_deg => complex.Phase.ToDegree();

        #region Regular and Static Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkParameter"/> structure from an existing <see cref="Complex"/> structure.
        /// </summary>
        /// <param name="complex">The existing <see cref="Complex"/> number.</param>
        public NetworkParameter(Complex complex) => this.complex = complex;
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkParameter"/> structure using the specified real and imaginary values.
        /// </summary>
        /// <param name="real">The real part of the complex number.</param>
        /// <param name="imaginary">The imaginary part of the complex number.</param>
        public NetworkParameter(double real, double imaginary)
        {
            complex = new Complex(real, imaginary);
        }
        /// <summary>Creates a new <see cref="NetworkParameter"/> from polar coordinates from a magnitude in decibels
        /// and a phase in degrees.</summary>
        /// <param name="magnitude_dB">The magnitude (absolute value) of the complex number in decibels (dB).</param>
        /// <param name="phase_deg">The phase of the complex number in degrees.</param>
        /// <returns>A new <see cref="NetworkParameter"/>.</returns>
        public static NetworkParameter FromPolarDecibelDegree(double magnitude_dB, double phase_deg)
        {
            double mag = magnitude_dB.ToLinear();
            double phase = phase_deg.ToRad();
            return new NetworkParameter(Complex.FromPolarCoordinates(mag, phase));
        }
        /// <summary>Creates a new <see cref="NetworkParameter"/> from polar coordinates from a magnitude and phase
        /// in degrees.</summary>
        /// <param name="magnitude">The magnitude (absolute value) of the complex number.</param>
        /// <param name="phase_deg">The phase of the complex number in degrees.</param>
        /// <returns>A new <see cref="NetworkParameter"/>.</returns>
        public static NetworkParameter FromPolarDegree(double magnitude, double phase_deg)
        {
            double phase = phase_deg.ToRad();
            return new NetworkParameter(Complex.FromPolarCoordinates(magnitude, phase));
        }
        /// <summary>Creates a new <see cref="NetworkParameter"/> from polar coordinates.</summary>
        /// <param name="magnitude">The magnitude (absolute value) of the complex number.</param>
        /// <param name="phase">The phase of the complex number in radians.</param>
        /// <returns>A new <see cref="NetworkParameter"/>.</returns>
        public static NetworkParameter FromPolar(double magnitude, double phase)
            => new NetworkParameter(Complex.FromPolarCoordinates(magnitude, phase));

        /// <summary>Returns a new <see cref="NetworkParameter"/> instance with a real number equal to zero
        /// and an imaginary number equal to zero.</summary>
        public static NetworkParameter Zero => new NetworkParameter(Complex.Zero);
        /// <summary>Returns a new <see cref="NetworkParameter"/> instance with a real number equal to one
        /// and an imaginary number equal to zero.</summary>
        public static NetworkParameter One => new NetworkParameter(Complex.One);
        #endregion

        #region Operators
        /// <summary>
        /// Converts a <see cref="NetworkParameter"/> to a <see cref="Complex"/> number.
        /// </summary>
        /// <param name="parameter">The parameter to convert.</param>
        public static implicit operator Complex(NetworkParameter parameter) => parameter.complex;

        /// <summary>
        /// Converts a <see cref="Complex"/> number to a <see cref="NetworkParameter"/>.
        /// </summary>
        /// <param name="complex">The complex number to convert.</param>
        public static implicit operator NetworkParameter(Complex complex) => new NetworkParameter(complex);

        /// <summary>
        /// Compares two <see cref="NetworkParameter"/> values for equality.
        /// </summary>
        /// <param name="left">The left <see cref="NetworkParameter"/>.</param>
        /// <param name="right">The right <see cref="NetworkParameter"/>.</param>
        /// <returns>True if <paramref name="left"/> equals <paramref name="right"/>; otherwise false.</returns>
        public static bool operator ==(NetworkParameter left, NetworkParameter right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="NetworkParameter"/> values for inequality.
        /// </summary>
        /// <param name="left">The left <see cref="NetworkParameter"/>.</param>
        /// <param name="right">The right <see cref="NetworkParameter"/>.</param>
        /// <returns>True if <paramref name="left"/> does not equal <paramref name="right"/>; otherwise false.</returns>
        public static bool operator !=(NetworkParameter left, NetworkParameter right) => !left.Equals(right);

        /// <summary>
        /// Returns the additive inverse of a specified network parameter.
        /// </summary>
        /// <param name="param">The network parameter.</param>
        /// <returns>The result of the multiplying <see cref="Real"/> and <see cref="Imaginary"/> by -1.</returns>
        public static NetworkParameter operator -(NetworkParameter param) => -param.complex;

        /// <summary>
        /// Adds two <see cref="NetworkParameter"/> values.
        /// </summary>
        /// <param name="left">The first value to add.</param>
        /// <param name="right">The second value to add.</param>
        /// <returns>The sum of left and right.</returns>
        public static NetworkParameter operator +(NetworkParameter left, NetworkParameter right) => left.complex + right.complex;

        /// <summary>
        /// Subtracts two <see cref="NetworkParameter"/> values.
        /// </summary>
        /// <param name="left">The first value to subtract.</param>
        /// <param name="right">The second value to subtract.</param>
        /// <returns>The result of subtracting right from left.</returns>
        public static NetworkParameter operator -(NetworkParameter left, NetworkParameter right) => left.complex - right.complex;

        /// <summary>
        /// Multiplies two <see cref="NetworkParameter"/> values.
        /// </summary>
        /// <param name="left">The first value to multiply.</param>
        /// <param name="right">The product of left and right.</param>
        /// <returns>The product of left and right.</returns>
        public static NetworkParameter operator *(NetworkParameter left, NetworkParameter right) => left.complex * right.complex;

        /// <summary>
        /// Divides a <see cref="NetworkParameter"/> value by another <see cref="NetworkParameter"/> value.
        /// </summary>
        /// <param name="left">The value to be divided.</param>
        /// <param name="right">The product of left and right.</param>
        /// <returns>The result of dividing left by right.</returns>
        public static NetworkParameter operator /(NetworkParameter left, NetworkParameter right) => left.complex / right.complex;
        #endregion

        #region Overrides

        /// <inheritdoc/>
        public override int GetHashCode() => complex.GetHashCode();

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj switch
            {
                NetworkParameter param => Equals(param),
                Complex complex => Equals(complex),
                _ => complex.Equals(obj),
            };
        }

        /// <summary>
        /// Converts the value of the current network parameter to its equivalent string representationin Cartesian form.
        /// </summary>
        /// <returns>The string representation of the current instance in Cartesian form.</returns>
        public override string ToString() => complex.ToString();

        /// 
        public string ToString(string format) => complex.ToString(format);
        #endregion

        #region Interface Implementations

        /// <inheritdoc/>
        public bool Equals(Complex other) => complex.Equals(other);

        /// <inheritdoc/>
        public bool Equals(NetworkParameter other) => complex.Equals(other.complex);
        #endregion
    }
    internal static class Extensions
    {
        public static double ToRad(this double angle_deg) => angle_deg * PI / 180;
        public static double ToDegree(this double angle_rad) => angle_rad * 180 / PI;
        public static double ToLinear(this double magnitude_dB) => Pow(10, magnitude_dB / 20);
        public static double FromLinear(this double magnitude) => 20 * Log10(magnitude);
    }
}
