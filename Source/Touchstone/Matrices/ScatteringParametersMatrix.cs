using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MicrowaveNetworks.Matrices
{
    public sealed class ScatteringParametersMatrix : NetworkParametersMatrix
    {
        public ScatteringParametersMatrix(int numPorts) : base(numPorts) { }


        public static explicit operator TransferParametersMatrix(ScatteringParametersMatrix s)
        {
            return new TransferParametersMatrix(s.NumPorts)
            {
                [1, 1] = -s.Determinant() / s[2, 1],
                [1, 2] = s[1, 1] / s[2, 1],
                [2, 1] = -s[2, 2] / s[2, 1],
                [2, 2] = NetworkParameter.One / s[2, 1]
            };

        }

        public static ScatteringParametersMatrix Cascade(params ScatteringParametersMatrix[] matrices)
        {
            return Cascade((IEnumerable<ScatteringParametersMatrix>)matrices);
        }
        public static ScatteringParametersMatrix Cascade(IEnumerable<ScatteringParametersMatrix> matrices)
        {
            if (matrices == Enumerable.Empty<ScatteringParametersMatrix>()) throw new ArgumentException(nameof(matrices));

            using (var enumer = matrices.GetEnumerator())
            {
                // Get the first element; we checked above for empty so we can safely get the first element
                enumer.MoveNext();

                ScatteringParametersMatrix s1 = enumer.Current;

                while (enumer.MoveNext())
                {
                    ScatteringParametersMatrix s2 = enumer.Current;

                    TransferParametersMatrix t1 = (TransferParametersMatrix)s1;
                    TransferParametersMatrix t2 = (TransferParametersMatrix)s2;

                    TransferParametersMatrix composite = t1 * t2;
                    s1 = (ScatteringParametersMatrix)composite;
                }
                return s1;
            }
        }
    }
}
