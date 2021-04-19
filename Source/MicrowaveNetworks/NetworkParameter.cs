using System;
using System.Numerics;
using static System.Math;

namespace MicrowaveNetworks
{
    public readonly struct NetworkParameter : IEquatable<Complex>, IEquatable<NetworkParameter>
    {
        private readonly Complex complex;

        public double Imaginary => complex.Imaginary;
        public double Real => complex.Real;
        public double Magnitude => complex.Magnitude;
        public double Magnitude_dB => complex.Magnitude.FromLinear();
        public double Phase => complex.Phase;
        public double Phase_deg => complex.Phase.ToDegree();

        private NetworkParameter(Complex complex) => this.complex = complex;
        public NetworkParameter(double real, double imaginary)
        {
            complex = new Complex(real, imaginary);
        }

        public static NetworkParameter FromPolarDecibelDegree(double magnitude_dB, double phase_deg)
        {
            double mag = magnitude_dB.ToLinear();
            double phase = phase_deg.ToRad();
            return new NetworkParameter(Complex.FromPolarCoordinates(mag, phase));
        }
        public static NetworkParameter FromPolarDegree(double magnitude, double phase_deg)
        {
            double phase = phase_deg.ToRad();
            return new NetworkParameter(Complex.FromPolarCoordinates(magnitude, phase));
        }
        public static NetworkParameter FromPolar(double magnitude, double phase)
            => new NetworkParameter(Complex.FromPolarCoordinates(magnitude, phase));


        public static NetworkParameter Zero => new NetworkParameter(Complex.Zero);
        public static NetworkParameter One => new NetworkParameter(Complex.One);

        #region Operators
        public static implicit operator Complex(NetworkParameter parameter) => parameter.complex;
        public static implicit operator NetworkParameter(Complex complex) => new NetworkParameter(complex);

        public static bool operator ==(NetworkParameter left, NetworkParameter right) => left.Equals(right);
        public static bool operator !=(NetworkParameter left, NetworkParameter right) => !left.Equals(right);

        public static NetworkParameter operator -(NetworkParameter param) => -param.complex;
        public static NetworkParameter operator +(NetworkParameter left, NetworkParameter right) => left.complex + right.complex;
        public static NetworkParameter operator -(NetworkParameter left, NetworkParameter right) => left.complex - right.complex;
        public static NetworkParameter operator *(NetworkParameter left, NetworkParameter right) => left.complex * right.complex;
        public static NetworkParameter operator /(NetworkParameter left, NetworkParameter right) => left.complex / right.complex;
        #endregion

        #region Overrides
        public override int GetHashCode() => complex.GetHashCode();
        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case NetworkParameter param:
                    return Equals(param);
                case Complex complex:
                    return Equals(complex);
                default:
                    return this.complex.Equals(obj);
            }
        }
        public override string ToString() => complex.ToString();
        public string ToString(string format) => complex.ToString(format);
        #endregion

        #region Interface Implementations
        public bool Equals(Complex other) => complex.Equals(other);
        public bool Equals(NetworkParameter other) => complex.Equals(other.complex);
        #endregion
    }
    internal static class Extensions
    {
        public static double ToRad(this double angle_deg) => angle_deg * PI / 180;
        public static double ToDegree(this double angle_rad) => angle_rad * 180 / PI;
        public static double ToLinear(this double magnitude_dB) => Pow(10, magnitude_dB / 10);
        public static double FromLinear(this double magnitude) => 10 * Log10(magnitude);
    }
}
