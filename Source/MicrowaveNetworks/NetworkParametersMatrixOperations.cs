using System;
using System.Collections.Generic;
using MicrowaveNetworks.Matrices;

namespace MicrowaveNetworks
{
    public abstract partial class NetworkParametersMatrix
    {
        /// <summary>
        /// Cascades (embeds) a series of <see cref="NetworkParametersMatrix"/> between subsequently connected ports to render a single composite
        /// <see cref="NetworkParametersMatrix"/> defining the relationship between the ports of the first device and the ports of the final device.
        /// </summary>
        /// <remarks>Cascading is performed by first converting the input matrices to <see cref="TransferParametersMatrix"/> types, and then using
        /// matrix multiplication to combine into a single T-parameter matrix. This matrix is then converted back to the original parameter matrix type.</remarks>
        /// <param name="matrices">An array of matrices to be cascaded.</param>
        /// <returns>A single composite <see cref="NetworkParameter"/> describing the network from the ports of the first device to the ports of the final device.</returns>
        public static NetworkParametersMatrix Cascade(params NetworkParametersMatrix[] matrices)
        {
            return Cascade((IList<NetworkParametersMatrix>)matrices);
        }
        /// <summary>
        /// Cascades (embeds) a series of <see cref="NetworkParametersMatrix"/> between subsequently connected ports to render a single composite
        /// <see cref="NetworkParametersMatrix"/> defining the relationship between the ports of the first device and the ports of the final device.
        /// </summary>
        /// <remarks>Cascading is performed by first converting the input matrices to <see cref="TransferParametersMatrix"/> types, and then using
        /// matrix multiplication to combine into a single T-parameter matrix. This matrix is then converted back to the original parameter matrix type.</remarks>
        /// <param name="matrices">A list of matrices to be cascaded.</param>
        /// <returns>A single composite <see cref="NetworkParameter"/> describing the network from the ports of the first device to the ports of the final device.</returns>
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
