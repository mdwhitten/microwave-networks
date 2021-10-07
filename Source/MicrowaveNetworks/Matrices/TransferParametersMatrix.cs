using MathNet.Numerics.LinearAlgebra.Complex;
using System;
namespace MicrowaveNetworks.Matrices
{
    using SymmetryExtension;

    /// <summary>
    /// Represents the transfer (T) parameters between device ports.
    /// </summary>
    public sealed class TransferParametersMatrix : NetworkParametersMatrix
    {
        /// <inheritdoc/>
        protected override string MatrixPrefix => "T";

        private TransferParametersMatrix(int numPorts, DenseMatrix matrix)
            : base(numPorts, matrix) { }

        /// <summary>
        /// Creates a new <see cref="TransferParametersMatrix"/> with the specified number of ports.
        /// </summary>
        public TransferParametersMatrix(int numPorts) : base(numPorts) { }

        internal TransferParametersMatrix(SymmetryMatrix symmetryMatrix)
            : base(symmetryMatrix.NumberOfPorts)
        {
            if (symmetryMatrix.NumberOfPorts == 2)
            {
                this[1, 1] = (NetworkParameterElement)(ISymmetryMatrixElement)symmetryMatrix.OneOne;
                this[1, 2] = (NetworkParameterElement)(ISymmetryMatrixElement)symmetryMatrix.OneTwo;
                this[2, 1] = (NetworkParameterElement)(ISymmetryMatrixElement)symmetryMatrix.TwoOne;
                this[2, 2] = (NetworkParameterElement)(ISymmetryMatrixElement)symmetryMatrix.TwoTwo;
            }
            else throw new NotImplementedException();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        protected override TransferParametersMatrix ToTParameters() => this;

        /// <summary>
        /// Converts the <see cref="TransferParametersMatrix"/> to a <see cref="ScatteringParametersMatrix"/>.
        /// </summary>
        /// <param name="t">The <see cref="TransferParametersMatrix"/> to convert.</param>
        public static explicit operator ScatteringParametersMatrix(TransferParametersMatrix t) => t.ToSParameters();

        /// <summary>
        /// Performs matrix multiplication on two <see cref="TransferParametersMatrix"/>, which is used for cascading network data.
        /// </summary>
        /// <param name="t1">The first matrix.</param>
        /// <param name="t2">The second matrix.</param>
        /// <returns>A new <see cref="TransferParametersMatrix"/> that is the result of the matrix multiplication of both matrices.</returns>
        public static TransferParametersMatrix operator *(TransferParametersMatrix t1, TransferParametersMatrix t2)
        {
            DenseMatrix m = t1.Matrix * t2.Matrix;
            int numPorts = t1.NumPorts > t2.NumPorts ? t1.NumPorts : t2.NumPorts;
            return new TransferParametersMatrix(numPorts, m);
        }
    }
}
