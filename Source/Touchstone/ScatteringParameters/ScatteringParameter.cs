using System;
using static System.Math;
using System.Numerics;

namespace Touchstone.ScatteringParameters
{
    /// <summary>
    /// Represents a single scattering parameter (S-parameter) for a given frequency, source port, and destination port.
    /// </summary>
    public readonly struct ScatteringParameter : IComparable<ScatteringParameter>, IComparable, IEquatable<ScatteringParameter>
    {
        public double Real { get; }
        public double Imaginary { get; }
        public double Magnitude_dB => Magnitude.FromLinear();
        public double Magnitude => Sqrt(Pow(Real, 2) + Pow(Imaginary, 2));
        public double Angle_deg => Atan2(Imaginary, Real).ToDegree();

        public ScatteringParameter(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }
        public static ScatteringParameter FromMagnitudeDecibelAngle(double magnitude_dB, double angle_deg)
        {
            double real = magnitude_dB.ToLinear() * Cos(angle_deg.ToRad());
            double imaginary = magnitude_dB.ToLinear() * Sin(angle_deg.ToRad());
            return new ScatteringParameter(real, imaginary);
        }
        public static ScatteringParameter FromMagnitudeAngle(double magnitude, double angle_deg)
        {
            double real = magnitude * Cos(angle_deg.ToRad());
            double imaginary = magnitude * Sin(angle_deg.ToRad());
            return new ScatteringParameter(real, imaginary);
        }

        public static ScatteringParameter Unity => new ScatteringParameter(1, 0);

        #region Conversions
        public static implicit operator Complex(ScatteringParameter s) => new Complex(s.Real, s.Imaginary);
        public static implicit operator ScatteringParameter(Complex c) => new Complex(c.Real, c.Imaginary);
        #endregion
        #region Overrides
        public override string ToString() => $"{Magnitude_dB:g3} dB, {Angle_deg:g3} deg";
        public override int GetHashCode() => ToString().GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is ScatteringParameter s2)
            {
                return Equals(s2);
            }
            else return false;
        }
        #endregion
        #region Operators
        public static bool operator ==(ScatteringParameter left, ScatteringParameter right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(ScatteringParameter left, ScatteringParameter right)
        {
            return !(left == right);
        }
        public static bool operator >(ScatteringParameter operand1, ScatteringParameter operand2)
        {
            return operand1.CompareTo(operand2) == 1;
        }
        public static bool operator <(ScatteringParameter operand1, ScatteringParameter operand2)
        {
            return operand1.CompareTo(operand2) == -1;
        }
        public static bool operator >=(ScatteringParameter operand1, ScatteringParameter operand2)
        {
            return operand1.CompareTo(operand2) >= 0;
        }
        public static bool operator <=(ScatteringParameter operand1, ScatteringParameter operand2)
        {
            return operand1.CompareTo(operand2) <= 0;
        }
        #endregion
        #region Interface Implementations
        public int CompareTo(ScatteringParameter other)
        {
            // Try to sort first based on magnitude
            int magPos = Magnitude.CompareTo(other.Magnitude);
            if (magPos != 0)
            {
                return magPos;
            }
            // If magnitudes are equal (magPos = 0), then sort based on angle instead
            else return Angle_deg.CompareTo(other.Angle_deg);
        }
        public int CompareTo(object obj)
        {
            if (obj is ScatteringParameter s2)
            {
                return CompareTo(s2);
            }
            else return 1;
        }
        public bool Equals(ScatteringParameter other) => Real == other.Real && Imaginary == other.Imaginary;
        #endregion
        public void Deconstruct(out double real, out double imaginary)
        {
            real = Real;
            imaginary = Imaginary;
        }
    }
    internal static class Extensions
    {
        public static double ToRad(this double angle_deg) => angle_deg * PI / 180;
        public static double ToDegree(this double angle_rad) => angle_rad * 180 / PI;
        public static double ToLinear(this double magnitude_dB) => Pow(10, magnitude_dB / 10);
        public static double FromLinear(this double magnitude) => 10 * Log10(magnitude);
    }
}
