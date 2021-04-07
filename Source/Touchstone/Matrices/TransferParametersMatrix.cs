using MathNet.Numerics.LinearAlgebra.Complex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicrowaveNetworks.Matrices
{
    public sealed class TransferParametersMatrix : NetworkParametersMatrix
    {
        private TransferParametersMatrix(int numPorts, DenseMatrix matrix)
            : base(numPorts, matrix) { }
        public TransferParametersMatrix(int numPorts) : base(numPorts) { }


        public static explicit operator ScatteringParametersMatrix(TransferParametersMatrix t)
        {
            return new ScatteringParametersMatrix(t.NumPorts)
            {
                [1, 1] = t[1, 2] / t[2, 2],
                [1, 2] = t.Determinant() / t[2, 2],
                [2, 1] = NetworkParameter.One / t[2, 2],
                [2, 2] = -t[2, 1] / t[2, 2]
            };
        }

        public static TransferParametersMatrix operator *(TransferParametersMatrix t1, TransferParametersMatrix t2)
        {
            DenseMatrix m = t1.matrix * t2.matrix;
            int numPorts = t1.NumPorts > t2.NumPorts ? t1.NumPorts : t2.NumPorts;
            return new TransferParametersMatrix(numPorts, m);
        }
    }
}
