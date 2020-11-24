using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace TouchstoneSnPFileReader
{
    public readonly struct FrequencyParametersPair
    {
        public double Frequency_Hz { get; }
        public DestinationPortCollection PortCollection { get; }

        public FrequencyParametersPair(double frequency, DestinationPortCollection portCollection)
        {
            Frequency_Hz = frequency;
            PortCollection = portCollection;
        }

        public static implicit operator FrequencyParametersPair(KeyValuePair<double, DestinationPortCollection> pair)
            => new FrequencyParametersPair(pair.Key, pair.Value);

        public void Deconstruct(out double frequency, out DestinationPortCollection portCollection)
        {
            frequency = Frequency_Hz;
            portCollection = PortCollection;
        }
    }

    public class ScatteringParametersCollection : IReadOnlyCollection<FrequencyParametersPair>
    {
        private Dictionary<double, DestinationPortCollection> _scatteringParameters;



        public DestinationPortCollection this[double frequency] => _scatteringParameters[frequency];

        public IEnumerable<double> Frequencies => _scatteringParameters.Keys;

        public IEnumerable<DestinationPortCollection> ScatteringParameters => _scatteringParameters.Values;

        public int Count => _scatteringParameters.Count;

        public bool ContainsFrequency(double frequency)
        {
            return _scatteringParameters.ContainsKey(frequency);
        }

        public IEnumerator<FrequencyParametersPair> GetEnumerator()
        {
            foreach (var pair in _scatteringParameters)
            {
                yield return pair;
            }
        }

        public bool TryGetValue(double frequency, out DestinationPortCollection value)
        {
            return _scatteringParameters.TryGetValue(frequency, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _scatteringParameters.GetEnumerator();
        }
    }
    public class DestinationPortCollection : IReadOnlyList<SourcePortCollection>
    {
        private List<SourcePortCollection> _parameterList;
        public int Count => _parameterList.Count;

        public IEnumerable<int> Ports
        {
            get
            {
                for (int i = 1; i <= Count; i++)
                {
                    yield return i;
                }
            }
        }

        public SourcePortCollection this[int destinationPort]
        {
            get => _parameterList[--destinationPort];
        }
        public ScatteringParameter this[(int destinationPort, int sourcePort) ports]
        {
            get => _parameterList[--ports.destinationPort][ports.sourcePort];
        }

        public IEnumerator<SourcePortCollection> GetEnumerator()
        {
            return _parameterList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _parameterList.GetEnumerator();
        }
    }
    public class SourcePortCollection : IReadOnlyList<ScatteringParameter>
    {
        private List<ScatteringParameter> _parameters;


        public ScatteringParameter this[int sourcePort]
        {
            get => _parameters[--sourcePort];
        }

        public int Count => _parameters.Count;

        public IEnumerable<int> Ports
        {
            get
            {
                for (int i = 1; i <= Count; i++)
                {
                    yield return i;
                }
            }
        }

        public IEnumerator<ScatteringParameter> GetEnumerator() => _parameters.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _parameters.GetEnumerator();
    }

    public class testClass
    {
        public void main()
        {
            DestinationPortCollection d = new DestinationPortCollection();

            ScatteringParameter s = d[2][1];
            ScatteringParameter s2 = d[(2, 1)];


            ScatteringParametersCollection f = new ScatteringParametersCollection();

            var result = from pair in f
                         let mag = pair.PortCollection[(2, 1)].Magnitude_dB
                         select (pair.Frequency_Hz, mag);

            foreach (var (frequency, ports) in f)
            {

            }
        }
    }
}

