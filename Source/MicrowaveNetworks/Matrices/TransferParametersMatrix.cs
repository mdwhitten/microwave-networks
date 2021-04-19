using MathNet.Numerics.LinearAlgebra.Complex;
using System;
namespace MicrowaveNetworks.Matrices
{
    using SymmetryExtension;
    using System.Numerics;
    /// <summary>
    /// Represents the transfer (T) parameters between device ports.
    /// </summary>
    public sealed class TransferParametersMatrix : NetworkParametersMatrix
    {
        private TransferParametersMatrix(int numPorts, DenseMatrix matrix)
            : base(numPorts, matrix) { }

        /// <summary>
        /// Creates a new <see cref="TransferParametersMatrix"/> with the specified number of ports.
        /// </summary>
        public TransferParametersMatrix(int numPorts) : base(numPorts) { }

        internal static  TransferParametersMatrix FromSymmetryMatrix<T>(SymmetryMatrix<T> symm)
        {
            TransferParametersMatrix t = new TransferParametersMatrix(symm.NumberOfPorts);

            if (symm.NumberOfPorts == 2 && symm is SymmetryMatrix<NetworkParameter> complex)
            {
                t[1, 1] = (NetworkParameter)complex[1, 1];
                t[1, 2] = complex.OneTwo;
                t[2, 1] = complex.TwoOne;
                t[2, 2] = complex.TwoTwo;
                return t;
            }
            else throw new NotImplementedException();

        }
        protected override ScatteringParametersMatrix ToSParameters()
        {
            return new ScatteringParametersMatrix(NumPorts)
            {
                [1, 1] = this[1, 2] / this[2, 2],
                [1, 2] = this.Determinant() / this[2, 2],
                [2, 1] = NetworkParameter.One / this[2, 2],
                [2, 2] = -this[2, 1] / this[2, 2]
            };
        }
        protected override TransferParametersMatrix ToTParameters() => this;

        public static explicit operator ScatteringParametersMatrix(TransferParametersMatrix t) => t.ToSParameters();

        public static TransferParametersMatrix operator *(TransferParametersMatrix t1, TransferParametersMatrix t2)
        {
            DenseMatrix m = t1.matrix * t2.matrix;
            int numPorts = t1.NumPorts > t2.NumPorts ? t1.NumPorts : t2.NumPorts;
            return new TransferParametersMatrix(numPorts, m);
        }
    }
}
