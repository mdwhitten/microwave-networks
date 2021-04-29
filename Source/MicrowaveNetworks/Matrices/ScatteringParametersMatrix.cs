using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra.Complex;

namespace MicrowaveNetworks.Matrices
{
    using SymmetryExtension;

    /// <summary>
    /// Represents the scattering (S) parameters between device ports.
    /// </summary>
    public sealed class ScatteringParametersMatrix : NetworkParametersMatrix
    {
        /// <summary>
        /// Creates a new <see cref="ScatteringParametersMatrix"/> with the specified number of ports.
        /// </summary>
        /// <param name="numPorts"></param>
        public ScatteringParametersMatrix(int numPorts) : base(numPorts) { }
        /// <summary>
        /// Creates a new <see cref="ScatteringParametersMatrix"/> from a flattened list of <see cref="NetworkParameter"/> structures.
        /// The list is assumed to be in <see cref="ListFormat.SourcePortMajor"/> format.
        /// </summary>
        /// <param name="flattenedList">The list of <see cref="NetworkParameter"/> objects to fill the matrix with. <para></para>
        /// The list is expected to be in <see cref="ListFormat.SourcePortMajor"/> format. The number of elements in <paramref name="flattenedList"/> 
        /// must be n^2, where n is the number of ports of the device. <see cref="NetworkParametersMatrix.NumPorts"/> will be set to n.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="flattenedList"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of elements in <paramref name="flattenedList"/> is not a perfect square.</exception>
        public ScatteringParametersMatrix(IList<NetworkParameter> flattenedList) : base(flattenedList) { }
        /// <summary>
        /// Creates a new <see cref="ScatteringParametersMatrix"/> from a flattened list of <see cref="NetworkParameter"/> structures in the specified format.</summary>
        /// <param name="flattenedList">The list of <see cref="NetworkParameter"/> objects to fill the matrix with. <para></para>
        /// The list is expected to be in <see cref="ListFormat.SourcePortMajor"/> format. The number of elements in <paramref name="flattenedList"/> 
        /// must be n^2, where n is the number of ports of the device. <see cref="NetworkParametersMatrix.NumPorts"/> will be set to n.</param>
        /// <param name="format">The format to be used for interperting the which element in the flat list correspods to the appropriate matrix index.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="flattenedList"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of elements in <paramref name="flattenedList"/> is not a perfect square.</exception>
        public ScatteringParametersMatrix(IList<NetworkParameter> flattenedList, ListFormat format) : base(flattenedList, format) { }

        public static explicit operator TransferParametersMatrix(ScatteringParametersMatrix s) => s.ToTParameters();

        protected override ScatteringParametersMatrix ToSParameters() => this;
        protected override TransferParametersMatrix ToTParameters()
        {
            SymmetryMatrix s = new SymmetryMatrix(NumPorts);
            switch (NumPorts)
            {
                case 1:
                    throw new InvalidOperationException("T parameter conversion invalid for single port network.");
                case 2:
                    return TwoPortStoT();
                case var _ when NumPorts % 2 == 0:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }

        private TransferParametersMatrix TwoPortStoT()
        {
            return new TransferParametersMatrix(NumPorts)
            {
                [1, 1] = -Determinant() / this[2, 1],
                [1, 2] = this[1, 1] / this[2, 1],
                [2, 1] = -this[2, 2] / this[2, 1],
                [2, 2] = Complex.Reciprocal(this[2, 1])
            };
        }


    }
}
