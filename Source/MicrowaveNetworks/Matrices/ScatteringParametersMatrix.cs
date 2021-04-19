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

    public sealed class ScatteringParametersMatrix : NetworkParametersMatrix
    {
        public ScatteringParametersMatrix(int numPorts) : base(numPorts) { }
        public ScatteringParametersMatrix(IList<NetworkParameter> flattendList, ListFormat format) : base(flattendList, format) { }

        public static explicit operator TransferParametersMatrix(ScatteringParametersMatrix s) => s.ToTParameters();

        protected override ScatteringParametersMatrix ToSParameters() => this;
        protected override TransferParametersMatrix ToTParameters()
        {
            //SymmetryMatrix s = new SymmetryMatrix(NumPorts);
            switch (NumPorts)
            {
                case 1:
                    throw new InvalidOperationException("T parameter conversion invalid for single port network.");
                case 2:
                    var a = SymmetryMatrix<NetworkParameter>.From2PortData(this);
                    var b = StoT(a);
                    return TransferParametersMatrix.FromSymmetryMatrix(b);
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
        private SymmetryMatrix<T> StoT<T>(SymmetryMatrix<T> s)
        {
            return new SymmetryMatrix<T>
            {
                NumberOfPorts = s.NumberOfPorts,
                [1, 1] = s[1, 2] - s[1, 1] * s[2, 1].Inverse() * s[2, 1],
                [1, 2] = s[1, 1] * s[2, 1].Inverse(),
                [2, 1] = -s[2, 1].Inverse() * s[2, 2],
                [2, 2] = s[2, 1].Inverse(),
            };
        }
    }
}
