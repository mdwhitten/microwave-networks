using System;
using System.Collections.Generic;
using MicrowaveNetworks.Matrices;
using System.Linq;
using MathNet.Numerics;

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
        public static TMatrix Cascade<TMatrix>(params TMatrix[] matrices) where TMatrix : NetworkParametersMatrix
        {
            return Cascade((IEnumerable<TMatrix>)matrices);
        }
        /// <summary>
        /// Cascades (embeds) a series of <see cref="NetworkParametersMatrix"/> between subsequently connected ports to render a single composite
        /// <see cref="NetworkParametersMatrix"/> defining the relationship between the ports of the first device and the ports of the final device.
        /// </summary>
        /// <remarks>Cascading is performed by first converting the input matrices to <see cref="TransferParametersMatrix"/> types, and then using
        /// matrix multiplication to combine into a single T-parameter matrix. This matrix is then converted back to the original parameter matrix type.</remarks>
        /// <param name="matrices">A list of matrices to be cascaded.</param>
        /// <returns>A single composite <see cref="NetworkParameter"/> describing the network from the ports of the first device to the ports of the final device.</returns>
        public static TMatrix Cascade<TMatrix>(IEnumerable<TMatrix> matrices) where TMatrix : NetworkParametersMatrix
        {
            if (matrices == null || !matrices.Any()) throw new ArgumentException("Argument cannot be null or empty", nameof(matrices));

            using IEnumerator<NetworkParametersMatrix> enumer = matrices.GetEnumerator();
            enumer.MoveNext();

            NetworkParametersMatrix p1 = enumer.Current;

            while (enumer.MoveNext())
            {
                NetworkParametersMatrix p2 = enumer.Current;

                TransferParametersMatrix t1 = p1.ToTParameters();
                TransferParametersMatrix t2 = p2.ToTParameters();

                TransferParametersMatrix composite = t1 * t2;

                p1 = composite;
            }

            return p1.ConvertParameterType<TMatrix>();
        }

        /// <summary>
        /// Deembeds (removes) the <see cref="NetworkParametersMatrix"/> from the input (left) side of the current matrix.
        /// </summary>
        /// <typeparam name="TMatrix">The type of <see cref="NetworkParametersMatrix"/>.</typeparam>
        /// <param name="left">The <see cref="NetworkParametersMatrix"/> to deembed from the current object.</param>
        /// <returns>A new <typeparamref name="TMatrix"/> with <paramref name="left"/> deembeded from the current object.</returns>
        /// <remarks>In T-parameters, this method computes: <code>T_d = T_Left^-1 * T_this</code></remarks>
		public TMatrix DeembedLeft<TMatrix>(TMatrix left) where TMatrix : NetworkParametersMatrix
        {
			TransferParametersMatrix t = this.ToTParameters();
			TransferParametersMatrix tLeftInv = left.ToTParameters().Inverse();
            TransferParametersMatrix deembed = tLeftInv * t;
			return deembed.ConvertParameterType<TMatrix>();
		}

		/// <summary>
		/// Deembeds (removes) the <see cref="NetworkParametersMatrix"/> from the output (right) side of the current matrix.
		/// </summary>
		/// <typeparam name="TMatrix">The type of <see cref="NetworkParametersMatrix"/>.</typeparam>
		/// <param name="right">The <see cref="NetworkParametersMatrix"/> to deembed from the current object.</param>
		/// <returns>A new <typeparamref name="TMatrix"/> with <paramref name="right"/> deembeded from the current object.</returns>
		/// <remarks>In T-parameters, this method computes: <code>T_d = T_this * T_Right^-1</code></remarks>
		public TMatrix DeembedRight<TMatrix>(TMatrix right) where TMatrix : NetworkParametersMatrix
		{
			TransferParametersMatrix t = this.ToTParameters();
			TransferParametersMatrix tRightInv = right.ToTParameters().Inverse();
			TransferParametersMatrix deembed = t * tRightInv;
			return deembed.ConvertParameterType<TMatrix>();
		}

		/// <summary>
		/// Deembeds (removes) the <see cref="NetworkParametersMatrix"/> from the input (left) and output (right) side of the current matrix.
		/// </summary>
		/// <typeparam name="TMatrix">The type of <see cref="NetworkParametersMatrix"/>.</typeparam>
		/// <param name="left">The <see cref="NetworkParametersMatrix"/> to deembed from the input (left) side current object.</param>
		/// <param name="right">The <see cref="NetworkParametersMatrix"/> to deembed from the output (right) side current object.</param>
		/// <returns>A new <typeparamref name="TMatrix"/> with <paramref name="right"/> deembeded from the current object.</returns>
		/// <remarks>In T-parameters, this method computes: <code>T_d = T_Left^-1 * T_this * T_Right^-1</code></remarks>
		public TMatrix Deembed<TMatrix>(TMatrix left, TMatrix right) where TMatrix : NetworkParametersMatrix
		{
            TransferParametersMatrix t = this.ToTParameters();
            TransferParametersMatrix tLeftInv = left.ToTParameters().Inverse();
			TransferParametersMatrix tRightInv = right.ToTParameters().Inverse();

			TransferParametersMatrix deembed = tLeftInv * t * tRightInv;
            return deembed.ConvertParameterType<TMatrix>();
		}
	}
}
