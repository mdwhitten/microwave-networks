using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using MicrowaveNetworks.Internal;
using MathNet.Numerics.LinearAlgebra.Complex;
using MicrowaveNetworks.Matrices;

namespace MicrowaveNetworks
{
    /// <summary>
    /// Defines a pair of a <see cref="MicrowaveNetworks.NetworkParameter"/> with its matrix indices.
    /// </summary>
    public readonly struct PortNetworkParameterPair
    {
        /// <summary>Gets the index (a <see cref="ValueTuple"/> with the source and destination ports) of <see cref="NetworkParameter"/>
        /// within the <see cref="NetworkParametersMatrix"/>.</summary>
        public (int DestinationPort, int SourcePort) Index { get; }
        /// <summary>Gets the <see cref="MicrowaveNetworks.NetworkParameter"/> specified at <see cref="Index"/>.</summary>
        public NetworkParameter NetworkParameter { get; }

        /// <summary>
        /// Creates a new structure from an index and network parameter.
        /// </summary>
        public PortNetworkParameterPair((int destPort, int sourcePort) index, NetworkParameter parameter)
        {
            Index = index;
            NetworkParameter = parameter;
        }

        /// <summary>Converts between a dictionary key/value pair structure to this one./></summary>
        public static implicit operator PortNetworkParameterPair(KeyValuePair<(int, int), NetworkParameter> pair)
            => new PortNetworkParameterPair(pair.Key, pair.Value);

        /// <summary>
        /// Provides support for using the ValueTuple deconstruction syntax for this structure, returning the network parameter and index.
        /// </summary>
        public void Deconstruct(out (int DestinationPort, int SourcePort) index, out NetworkParameter parameter)
        {
            index = Index;
            parameter = NetworkParameter;
        }
    }
    /// <summary>Defines how to interpret or return a <see cref="NetworkParametersMatrix"/> in a flattened list format.</summary>
    public enum ListFormat
    {
        /// <summary>Indicates that the source port (i.e. column) will be held constant while each destination port (i.e. row) will be enumerated.</summary>
        /// <remarks>For example, a matrix rendered in this format would have the following ordered indices:<para></para>
        /// [1, 1], [2, 1], ... [n, 1], [1, 2] ... [n, n]</remarks>
        SourcePortMajor,
        /// <summary>Indicates that the destination port (i.e. row) will be held constant while each source port (i.e. column) will be enumerated.</summary>
        /// <remarks>For example, a matrix rendered in this format would have the following ordered indices:<para></para>
        /// [1, 1], [1, 2], ... [1, n], [2, 1] ... [n, n]</remarks>
        DestinationPortMajor
    }
    /// <summary>
    /// A matrix that describes the electrical behavior between multiple ports of a device. Child classes of this abstract class implement 
    /// conversions to and from various different types of network parameter matrices, such as scattering parameters, transfer parameters, etc.
    /// </summary>
    public abstract partial class NetworkParametersMatrix : IEnumerable<PortNetworkParameterPair>
    {
        protected DenseMatrix matrix;

        /// <summary>Gets the number of ports of the device represented by this matrix.</summary>
        public int NumPorts { get; private set; }

        #region Constructors
        protected NetworkParametersMatrix(int numPorts, DenseMatrix matrix)
        {
            NumPorts = numPorts;
            this.matrix = matrix;
        }
        /// <summary>
        /// Creates a new <see cref="NetworkParametersMatrix"/> with the specified number of ports.
        /// </summary>
        /// <param name="numPorts">The numer of ports of the device represented by this matrix.</param>
        protected NetworkParametersMatrix(int numPorts)
        {
            NumPorts = numPorts;
            //parametersMatrix = new Dictionary<(int destPort, int sourcePort), NetworkParameter>(m_size);
            matrix = DenseMatrix.Create(numPorts, numPorts, Complex.One);
        }
        /// <summary>
        /// Creates a new <see cref="NetworkParametersMatrix"/> from a flattened list of <see cref="NetworkParameter"/> structures.
        /// The list is assumed to be in <see cref="ListFormat.SourcePortMajor"/> format.
        /// </summary>
        /// <param name="flattenedList">The list of <see cref="NetworkParameter"/> structures to fill the matrix with. <para></para>
        /// The list is expected to be in <see cref="ListFormat.SourcePortMajor"/> format. The number of elements in <paramref name="flattenedList"/> 
        /// must be n^2, where n is the number of ports of the device. <see cref="NumPorts"/> will be set to n.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="flattenedList"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of elements in <paramref name="flattenedList"/> is not a perfect square.</exception>
        protected NetworkParametersMatrix(IList<NetworkParameter> flattenedList)
        {
            CreateFromFlattenedList(flattenedList, ListFormat.SourcePortMajor);
        }
        /// <summary>
        /// Creates a new <see cref="NetworkParametersMatrix"/> from a flattened list of <see cref="NetworkParameter"/> structures based
        /// on the list format defined by <paramref name="format"/>.
        /// </summary>
        /// <param name="flattenedList">The list of <see cref="NetworkParameter"/> structures to fill the matrix with. <para></para>
        /// The number of elements in <paramref name="flattenedList"/> must be n^2, where n is the number of ports of the device. 
        /// <see cref="NumPorts"/> will be set to n.</param>
        /// <param name="format">The format to be used for interperting the which element in the flat list correspods to the appropriate matrix index.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="flattenedList"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the number of elements in <paramref name="flattenedList"/> is not a perfect square.</exception>
        protected NetworkParametersMatrix(IList<NetworkParameter> flattenedList, ListFormat format)
        {
            CreateFromFlattenedList(flattenedList, format);
        }

        private void CreateFromFlattenedList(IList<NetworkParameter> flattenedList, ListFormat format)
        {
            if (flattenedList == null) throw new ArgumentNullException(nameof(flattenedList));

            if (!flattenedList.Count.IsPerfectSquare(out int ports))
                throw new ArgumentOutOfRangeException(nameof(flattenedList), "List must contain (num-ports) squared elements.");

            NumPorts = ports;
            //parametersMatrix = new Dictionary<(int destPort, int sourcePort), NetworkParameter>();
            matrix = DenseMatrix.Create(ports, ports, Complex.Zero);


            int i = 0;
            Utilities.ForEachParameter(NumPorts, format, index =>
            {
                int row = index.DestinationPort - 1;
                int column = index.SourcePort - 1;
                matrix[row, column] = flattenedList[i];
                i++;
            });
        }
        #endregion
        /// <summary>Returns the <see cref="NetworkParameter"/> at the specified matrix indices.</summary>
        /// <param name="destinationPort">The index of the destination port (row). This value is one-indexed.</param>
        /// <param name="sourcePort">The index of the source port (column). This value is one-indexed.</param>
        /// <returns>The <see cref="NetworkParameter"/> at the specified indices.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if either index is less than one or greather than <see cref="NumPorts"/>.</exception>
        public NetworkParameter this[int destinationPort, int sourcePort]
        {
            get
            {
                ValidateIndicies(destinationPort, sourcePort);
                /*bool exists = parametersMatrix.TryGetValue((destinationPort, sourcePort), out NetworkParameter s);
                if (exists) return s;
                // Rather than filling the whole matrix with the unity parameter, simply return unity whenever the value for the requested
                // port has not been set.
                else return NetworkParameter.One;*/
                return matrix[destinationPort - 1, sourcePort - 1];
            }
            set
            {
                ValidateIndicies(destinationPort, sourcePort);
                //parametersMatrix[(destinationPort, sourcePort)] = value;
                matrix[destinationPort - 1, sourcePort - 1] = value;
            }
        }
               
        /// <summary>Converts this <see cref="NetworkParametersMatrix"/> to the <see cref="NetworkParametersMatrix"/> type specified by <typeparamref name="T"/>.</summary>
        /// <remarks>Each child class is required to define matrix conversion functions to all other parameter matrix types. If the requested type is the same as the current type,
        /// the same object will simply be returned.</remarks>
        /// <typeparam name="T">The type of <see cref="NetworkParametersMatrix"/> to convert this object to. Must be a child class of <see cref="NetworkParametersMatrix"/>.</typeparam>
        /// <returns>A <see cref="NetworkParametersMatrix"/> of type <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> is <see cref="NetworkParametersMatrix"/> rather than a child class.</exception>
        public T ConvertParameterType<T>() where T : NetworkParametersMatrix
        {
            Type tType = typeof(T);

            return (T)ConvertParameterType(tType);
        }

        #region Overrides and Interface Implementations
        /// <summary>Returns an enumerator for the matrix with a default <see cref="ListFormat"/> of <see cref="ListFormat.SourcePortMajor"/>.</summary>
        /// <returns>An enumerator for the matrix.</returns>
        public IEnumerator<PortNetworkParameterPair> GetEnumerator() => GetEnumerator(ListFormat.SourcePortMajor);
        /// <summary>Returns an enumerator for the matrix with element ordering defined by <paramref name="format"/>.</summary>
        /// <returns>An enumerator for the matrix in the specified format.</returns>
        public IEnumerator<PortNetworkParameterPair> GetEnumerator(ListFormat format)
        {
            return EnumerateParameters(format).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        /// <summary>
        /// Enumerates the parameters in the matrix based on the specified format.
        /// </summary>
        /// <param name="format">The <see cref="ListFormat"/> defining what order the parameters should be returned in./</param>
        /// <returns>A sequence of <see cref="PortNetworkParameterPair"/> values.</returns>
        public IEnumerable<PortNetworkParameterPair> EnumerateParameters(ListFormat format = ListFormat.DestinationPortMajor)
        {
            return Utilities.ForEachParameter(NumPorts, format, index =>
                                    new PortNetworkParameterPair(index, this[index.DestinationPort, index.SourcePort]));
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Utilities.ForEachParameter(NumPorts, ListFormat.DestinationPortMajor, index =>
            {
                (int dest, int source) = index;

                // When row starts, add a new '['
                if (source == 1) sb.Append('[');
                sb.Append($"S{dest}{source}: {this[dest, source]}");
                // End of row - start the next
                if (source == NumPorts) sb.AppendLine("]");
                else sb.Append('\t');

            });
            return sb.ToString();
        }
        #endregion
        #region Internal Functions
        /// <summary>
        /// Returns the determinant of the matrix.
        /// </summary>
        /// <returns></returns>
        protected NetworkParameter Determinant() => matrix.Determinant();
        private void ValidateIndicies(int destinationPort, int sourcePort)
        {
            bool invalid = false;
            string paramName = "";

            if (destinationPort <= 0 || destinationPort > NumPorts)
            {
                invalid = true;
                paramName = nameof(destinationPort);
            }
            else if (sourcePort <= 0 || sourcePort > NumPorts)
            {
                invalid = true;
                paramName = nameof(sourcePort);
            }
            if (invalid)
            {
                throw new IndexOutOfRangeException($"Invalid index specified for {paramName}. Valid values are from 1 to {NumPorts}.");
            }
        }
        private NetworkParametersMatrix ConvertParameterType(Type parameterType)
        {
            switch (true)
            {
                case true when parameterType == typeof(ScatteringParametersMatrix):
                    return ToSParameters();
                case true when parameterType == typeof(TransferParametersMatrix):
                    return ToTParameters();
                case true when parameterType == typeof(NetworkParametersMatrix):
                    throw new ArgumentException($"Conversion type must be a child class of {nameof(NetworkParametersMatrix)}.", nameof(parameterType));
                default:
                    throw new NotImplementedException();
            };
        }
        protected abstract ScatteringParametersMatrix ToSParameters();
        protected abstract TransferParametersMatrix ToTParameters();
        #endregion
    }
}
