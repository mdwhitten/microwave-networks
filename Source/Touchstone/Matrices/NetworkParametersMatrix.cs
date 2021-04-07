using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using Touchstone.Internal;

namespace MicrowaveNetworks
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
    public class NetworkParametersMatrix : IEnumerable<PortNetworkParameterPair>
    {
        private Dictionary<(int destPort, int sourcePort), NetworkParameter> parametersMatrix;

        public int NumPorts { get; private set; }

        public NetworkParametersMatrix(int numPorts)
        {
            int m_size = numPorts * numPorts;
            NumPorts = numPorts;
            parametersMatrix = new Dictionary<(int destPort, int sourcePort), NetworkParameter>(m_size);
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
            parametersMatrix = new Dictionary<(int destPort, int sourcePort), NetworkParameter>();

            using (var enumer = flattenedList.GetEnumerator())
            {
                for (int i = 1; i <= NumPorts; i++)
                {
                    for (int j = 1; j <= NumPorts; j++)
                    {
                        enumer.MoveNext();
                        NetworkParameter s = enumer.Current;

                        if (format == ListFormat.SourcePortMajor)
                        {
                            parametersMatrix[(i, j)] = s;
                        }
                        else parametersMatrix[(j, i)] = s;
                    }
                }
            }
        }

        public NetworkParameter this[int destinationPort, int sourcePort]
        {
            get
            {
                ValidateIndicies(destinationPort, sourcePort);
                bool exists = parametersMatrix.TryGetValue((destinationPort, sourcePort), out NetworkParameter s);
                if (exists) return s;
                // Rather than filling the whole matrix with the unity parameter, simply return unity whenever the value for the requested
                // port has not been set.
                else return NetworkParameter.One;
            }
            set
            {
                ValidateIndicies(destinationPort, sourcePort);
                parametersMatrix[(destinationPort, sourcePort)] = value;
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
            for (int i = 1; i <= NumPorts; i++)
            {
                for (int j = 1; j <= NumPorts; j++)
                {
                    NetworkParameter s = format == ListFormat.SourcePortMajor ? this[i, j] : this[j, i];
                    yield return new PortNetworkParameterPair((i, j), s);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i <= NumPorts; i++)
            {
                sb.Append("[");
                for (int j = 1; j <= NumPorts; j++)
                {
                    sb.Append($"S{i}{j}: {this[i, j]}\t");
                }
                sb.AppendLine("]");
            }/*
            foreach (var parameters in this)
            {
                (int dest, int source) = parameters.Index;
                sb.AppendLine($"[{dest}, {source}] = {parameters.NetworkParameter}");
            }*/
            return sb.ToString();
        }
    }
}
