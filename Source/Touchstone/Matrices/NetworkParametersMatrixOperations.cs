using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicrowaveNetworks.Matrices
{
    public abstract partial class NetworkParametersMatrix
    {
        public static NetworkParametersMatrix Cascade(params NetworkParametersMatrix[] matrices)
        {
            return Cascade((IList<NetworkParametersMatrix>)matrices);
        }
        public static NetworkParametersMatrix Cascade(IList<NetworkParametersMatrix> matrices)
        {
            if (matrices == null || matrices.Count <= 0) throw new ArgumentException(nameof(matrices));

            //HashSet<Type> set = new HashSet<Type>(matrices.Select( m => m.GetType())
            NetworkParametersMatrix p1 = matrices[0];
            Type firstType = p1.GetType();

            for (int i = 1; i < matrices.Count; i++)
            {
                NetworkParametersMatrix p2 = matrices[i];

                TransferParametersMatrix t1 = p1.ToTParameters();
                TransferParametersMatrix t2 = p2.ToTParameters();

                TransferParametersMatrix composite = t1 * t2;

                p1 = composite;
            }

            return p1.ConvertParameterType(firstType);
        }
    }
}
