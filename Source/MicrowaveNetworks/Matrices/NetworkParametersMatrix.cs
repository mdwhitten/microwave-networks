using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using MicrowaveNetworks.Internal;
using MathNet.Numerics.LinearAlgebra.Complex;

namespace MicrowaveNetworks.Matrices
{
    public readonly struct PortNetworkParameterPair
    {
        public (int DestinationPort, int SourcePort) Index { get; }
        public NetworkParameter NetworkParameter { get; }

        public PortNetworkParameterPair((int destPort, int sourcePort) index, NetworkParameter parameter)
        {
            Index = index;
            NetworkParameter = parameter;
        }

        public static implicit operator PortNetworkParameterPair(KeyValuePair<(int, int), NetworkParameter> pair)
            => new PortNetworkParameterPair(pair.Key, pair.Value);

        public void Deconstruct(out (int DestinationPort, int SourcePort) index, out NetworkParameter parameter)
        {
            index = Index;
            parameter = NetworkParameter;
        }
    }
    public enum ListFormat
    {
        SourcePortMajor,
        DestinationPortMajor
    }
    public abstract partial class NetworkParametersMatrix : IEnumerable<PortNetworkParameterPair>
    {
        //private Dictionary<(int destPort, int sourcePort), NetworkParameter> parametersMatrix;

        protected DenseMatrix matrix;

        public int NumPorts { get; private set; }

        protected NetworkParametersMatrix(int numPorts, DenseMatrix matrix)
        {
            NumPorts = numPorts;
            this.matrix = matrix;
        }
        public NetworkParametersMatrix(int numPorts)
        {
            NumPorts = numPorts;
            //parametersMatrix = new Dictionary<(int destPort, int sourcePort), NetworkParameter>(m_size);
            matrix = DenseMatrix.Create(numPorts, numPorts, Complex.Zero);
        }
        public NetworkParametersMatrix(IList<NetworkParameter> flattendList)
        {
            CreateFromFlattenedList(flattendList, ListFormat.SourcePortMajor);
        }
        public NetworkParametersMatrix(IList<NetworkParameter> flattenedList, ListFormat format)
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
                throw new ArgumentOutOfRangeException(paramName, $"Invalid port specified. Valid values are from 1 to {NumPorts}.");
            }
        }

        public IEnumerator<PortNetworkParameterPair> GetEnumerator() => GetEnumerator(ListFormat.SourcePortMajor);
        public IEnumerator<PortNetworkParameterPair> GetEnumerator(ListFormat format)
        {
            // Custom index the object instead of just returning the dictionary key/value pairs.
            // This handles the case when only one parameter (say, s21) has been set for the matrix.
            // No other values are configured for the matrix so they do not exist in the dictionary; hence,
            // only one value would be returned. Instead, we want to return unity for all non-configured parameters,
            // which is what the indexer above does.
            var parameters = Utilities.ForEachParameter(NumPorts, format, index => new PortNetworkParameterPair(index, this[index.DestinationPort, index.SourcePort]));
            return parameters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Utilities.ForEachParameter(NumPorts, ListFormat.DestinationPortMajor, index =>
            {
                (int dest, int source) = index;

                // When row starts, add a new '['
                if (source == 1) sb.Append("[");
                sb.Append($"S{dest}{source}: {this[dest, source]}");
                // End of row - start the next
                if (source == NumPorts) sb.AppendLine("]");
                else sb.Append("\t");

            });
            return sb.ToString();
        }

        protected NetworkParameter Determinant() => matrix.Determinant();
        private NetworkParametersMatrix ConvertParameterType(Type parameterType)
        {
            switch (true)
            {
                case true when parameterType == typeof(ScatteringParametersMatrix):
                    return ToSParameters();
                case true when parameterType == typeof(TransferParametersMatrix):
                    return ToTParameters();
                default:
                    throw new NotImplementedException();
            };
        }

        public T ConvertParameterType<T>() where T : NetworkParametersMatrix
        {
            Type tType = typeof(T);

            return (T)ConvertParameterType(tType);
        }

        protected abstract ScatteringParametersMatrix ToSParameters();
        protected abstract TransferParametersMatrix ToTParameters();

    }
}
