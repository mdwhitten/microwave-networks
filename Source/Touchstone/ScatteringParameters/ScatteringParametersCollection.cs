using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Touchstone.ScatteringParameters
{
    public readonly struct FrequencyParametersPair
    {
        public double Frequency_Hz { get; }
        public ScatteringParametersMatrix Parameters { get; }

        public FrequencyParametersPair(double frequency, ScatteringParametersMatrix parameters)
        {
            Frequency_Hz = frequency;
            Parameters = parameters;
        }

        public static implicit operator FrequencyParametersPair(KeyValuePair<double, ScatteringParametersMatrix> pair)
            => new FrequencyParametersPair(pair.Key, pair.Value);
        public static implicit operator KeyValuePair<double, ScatteringParametersMatrix>(FrequencyParametersPair pair)
            => new KeyValuePair<double, ScatteringParametersMatrix>(pair.Frequency_Hz, pair.Parameters);

        public void Deconstruct(out double frequency, out ScatteringParametersMatrix parameters)
        {
            frequency = Frequency_Hz;
            parameters = Parameters;
        }
    }

    public class ScatteringParametersCollection : ICollection<FrequencyParametersPair>
    {
        private Dictionary<double, ScatteringParametersMatrix> _scatteringParameters = new Dictionary<double, ScatteringParametersMatrix>();

        public int NumberOfPorts { get; }

        public ICollection<double> Frequencies => _scatteringParameters.Keys;
        public ICollection<ScatteringParametersMatrix> ScatteringParameters => _scatteringParameters.Values;

        public ScatteringParametersCollection(int numPorts)
        {
            NumberOfPorts = numPorts;
        }

        public ScatteringParametersMatrix this[double frequency]
        {
            get => _scatteringParameters[frequency];
            set => _scatteringParameters[frequency] = value;
        }
        public ScatteringParameter this[double frequency, int destinationPort, int sourcePort]
        {
            get => _scatteringParameters[frequency][destinationPort, sourcePort];
            set
            {
                bool exists = _scatteringParameters.TryGetValue(frequency, out ScatteringParametersMatrix parameters);
                if (exists)
                {
                    parameters[destinationPort, sourcePort] = value;
                }
                else
                {
                    ScatteringParametersMatrix matrix = new ScatteringParametersMatrix(NumberOfPorts);
                    matrix[destinationPort, sourcePort] = value;
                    _scatteringParameters.Add(frequency, matrix);
                }
            }
        }

        public int Count => _scatteringParameters.Count;

        public bool ContainsFrequency(double frequency) => _scatteringParameters.ContainsKey(frequency);
        public void Add(double frequency, ScatteringParametersMatrix parameters) => _scatteringParameters.Add(frequency, parameters);
        public void Clear() =>  _scatteringParameters.Clear();
        public bool Remove(double frequency) => _scatteringParameters.Remove(frequency);
        public bool TryGetValue(double frequency, out ScatteringParametersMatrix parameters) => _scatteringParameters.TryGetValue(frequency, out parameters);
        public IEnumerator<FrequencyParametersPair> GetEnumerator()
        {
            foreach (var pair in _scatteringParameters)
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
            => ((ICollection<KeyValuePair<double, ScatteringParametersMatrix>>)_scatteringParameters).Contains(item);

        void ICollection<FrequencyParametersPair>.CopyTo(FrequencyParametersPair[] array, int arrayIndex)
        {
            var arr = Array.ConvertAll(array, f => (KeyValuePair<double, ScatteringParametersMatrix>)f);
            ((ICollection<KeyValuePair<double, ScatteringParametersMatrix>>)_scatteringParameters).CopyTo(arr, arrayIndex);
        }

        bool ICollection<FrequencyParametersPair>.Remove(FrequencyParametersPair item)
        {
            return ((ICollection<KeyValuePair<double, ScatteringParametersMatrix>>)_scatteringParameters).Remove(item);
        }
        #endregion

    }



    public class testClass
    {
        public void main()
        {
            ScatteringParametersMatrix d = new ScatteringParametersMatrix(2);

            ScatteringParameter s = d[3, 1];
            ScatteringParameter s2 = d[2, 1];

            ScatteringParametersCollection f = new ScatteringParametersCollection(2);

            var result = from pair in f
                         let mag = pair.Parameters[2, 1].Magnitude_dB
                         select (pair.Frequency_Hz, mag);

            var result2 = from freq in f.Frequencies
                          select (freq, f[freq, 2, 1].Magnitude_dB);
            foreach (var (frequency, ports) in f)
            {

            }
            TouchstoneNetworkData file = new TouchstoneNetworkData(2, null);

            file.ScatteringParameters = new ScatteringParametersCollection(2)
            {
                [2e9, 2, 1] = ScatteringParameter.FromMagnitudeDecibelAngle(.5, 0),
                [2e9] = new ScatteringParametersMatrix(2) { [1, 1] = ScatteringParameter.Unity, [2, 2] = ScatteringParameter.Unity },

            };
            //file.ScatteringParameters[]
        }
    }
}

