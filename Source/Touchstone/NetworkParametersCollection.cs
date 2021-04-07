using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MicrowaveNetworks.Matrices;

namespace MicrowaveNetworks
{
    public readonly struct FrequencyParametersPair
    {
        public double Frequency_Hz { get; }
        public NetworkParametersMatrix Parameters { get; }

        public FrequencyParametersPair(double frequency, NetworkParametersMatrix parameters)
        {
            Frequency_Hz = frequency;
            Parameters = parameters;
        }

        public static implicit operator FrequencyParametersPair(KeyValuePair<double, NetworkParametersMatrix> pair)
            => new FrequencyParametersPair(pair.Key, pair.Value);
        public static implicit operator KeyValuePair<double, NetworkParametersMatrix>(FrequencyParametersPair pair)
            => new KeyValuePair<double, NetworkParametersMatrix>(pair.Frequency_Hz, pair.Parameters);

        public void Deconstruct(out double frequency, out NetworkParametersMatrix parameters)
        {
            frequency = Frequency_Hz;
            parameters = Parameters;
        }
    }

    public sealed class NetworkParametersCollection : ICollection<FrequencyParametersPair>
    {
        private Dictionary<double, NetworkParametersMatrix> _NetworkParameters = new Dictionary<double, NetworkParametersMatrix>();

        public int NumberOfPorts { get; }

        public ICollection<double> Frequencies => _NetworkParameters.Keys;
        public ICollection<NetworkParametersMatrix> NetworkParameters => _NetworkParameters.Values;

        public NetworkParametersCollection(int numPorts)
        {
            NumberOfPorts = numPorts;
        }

        public NetworkParametersMatrix this[double frequency]
        {
            get => _NetworkParameters[frequency];
            set => _NetworkParameters[frequency] = value;
        }
        public NetworkParameter this[double frequency, int destinationPort, int sourcePort]
        {
            get => _NetworkParameters[frequency][destinationPort, sourcePort];
            set
            {
                bool exists = _NetworkParameters.TryGetValue(frequency, out NetworkParametersMatrix parameters);
                if (exists)
                {
                    parameters[destinationPort, sourcePort] = value;
                }
                else
                {
//TODO
                    throw new NotImplementedException();
                    //NetworkParametersMatrix matrix = new NetworkParametersMatrix(NumberOfPorts);
                    //matrix[destinationPort, sourcePort] = value;
                    //_NetworkParameters.Add(frequency, matrix);
                }
            }
        }

        public int Count => _NetworkParameters.Count;

        public bool ContainsFrequency(double frequency) => _NetworkParameters.ContainsKey(frequency);
        public void Add(double frequency, NetworkParametersMatrix parameters) => _NetworkParameters.Add(frequency, parameters);
        public void Clear() => _NetworkParameters.Clear();
        public bool Remove(double frequency) => _NetworkParameters.Remove(frequency);
        public bool TryGetValue(double frequency, out NetworkParametersMatrix parameters) => _NetworkParameters.TryGetValue(frequency, out parameters);
        public IEnumerator<FrequencyParametersPair> GetEnumerator()
        {
            foreach (var pair in _NetworkParameters)
            {
                yield return pair;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region Explicit ICollection Implementations
        bool ICollection<FrequencyParametersPair>.IsReadOnly => false;
        void ICollection<FrequencyParametersPair>.Add(FrequencyParametersPair item)
            => Add(item.Frequency_Hz, item.Parameters);

        bool ICollection<FrequencyParametersPair>.Contains(FrequencyParametersPair item)
            => ((ICollection<KeyValuePair<double, NetworkParametersMatrix>>)_NetworkParameters).Contains(item);

        void ICollection<FrequencyParametersPair>.CopyTo(FrequencyParametersPair[] array, int arrayIndex)
        {
            var arr = Array.ConvertAll(array, f => (KeyValuePair<double, NetworkParametersMatrix>)f);
            ((ICollection<KeyValuePair<double, NetworkParametersMatrix>>)_NetworkParameters).CopyTo(arr, arrayIndex);
        }

        bool ICollection<FrequencyParametersPair>.Remove(FrequencyParametersPair item)
        {
            return ((ICollection<KeyValuePair<double, NetworkParametersMatrix>>)_NetworkParameters).Remove(item);
        }
        #endregion

    }
}

