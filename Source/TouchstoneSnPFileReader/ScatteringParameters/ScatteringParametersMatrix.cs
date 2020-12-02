﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace TouchstoneSnPFileReader.ScatteringParameters
{
    public readonly struct PortScatteringParameterPair
    {
        public (int DestinationPort, int SourcePort) Index { get; }
        public ScatteringParameter ScatteringParameter { get; }

        public PortScatteringParameterPair((int destPort, int sourcePort) index, ScatteringParameter s)
        {
            Index = index;
            ScatteringParameter = s;
        }

        public static implicit operator PortScatteringParameterPair(KeyValuePair<(int, int), ScatteringParameter> pair)
            => new PortScatteringParameterPair(pair.Key, pair.Value);
    }
    public class ScatteringParametersMatrix : IEnumerable<PortScatteringParameterPair>
    {
        private Dictionary<(int destPort, int sourcePort), ScatteringParameter> _sMatrix;

        public int NumPorts { get; }

        public ScatteringParametersMatrix(int numPorts)
        {
            int m_size = numPorts * numPorts;
            NumPorts = numPorts;
            _sMatrix = new Dictionary<(int destPort, int sourcePort), ScatteringParameter>(numPorts);
        }

        public ScatteringParameter this[int destinationPort, int sourcePort]
        {
            get
            {
                ValidateIndicies(destinationPort, sourcePort);
                bool exists = _sMatrix.TryGetValue((destinationPort, sourcePort), out ScatteringParameter s);
                if (exists) return s;
                // Rather than filling the whole matrix with the unity parameter, simply return unity whenever the value for the requested
                // port has not been set.
                else return ScatteringParameter.Unity;
            }
            set
            {
                ValidateIndicies(destinationPort, sourcePort);
                _sMatrix[(destinationPort, sourcePort)] = value;
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

        public IEnumerator<PortScatteringParameterPair> GetEnumerator()
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
                    ScatteringParameter s = this[i, j];
                    yield return new PortScatteringParameterPair((i, j), s);
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
            foreach (var parameters in this)
            {
                (int dest, int source) = parameters.Index;
                sb.AppendLine($"[{dest}, {source}] = {parameters.ScatteringParameter}");
            }
            return sb.ToString();
        }
    }
}
