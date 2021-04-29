using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace MicrowaveNetworks
{
    /// <summary>
    /// Contains a network parameter matrix and the measurement frequency associated with the matrix.
    /// </summary>
    public readonly struct FrequencyParametersPair
    {
        /// <summary>Gets the measurement or derived frequency in Hz.</summary>
        public double Frequency_Hz { get; }
        /// <summary>Gets the <see cref="NetworkParametersMatrix"/> measured or derived at <see cref="Frequency_Hz"/>.</summary>
        public NetworkParametersMatrix Parameters { get; }

        /// <summary>Creates a new <see cref="FrequencyParametersPair"/> associated a network parameters matrix and the measurement/derived frequency.</summary>
        /// <param name="frequency">The frequency in Hz.</param>
        /// <param name="parameters">The network parameters matrix.</param>
        public FrequencyParametersPair(double frequency, NetworkParametersMatrix parameters)
        {
            Frequency_Hz = frequency;
            Parameters = parameters;
        }

        /// <summary>Creates a <see cref="FrequencyParametersPair"/> from a <see cref="KeyValuePair{TKey, TValue}"/> object.</summary>
        public static implicit operator FrequencyParametersPair(KeyValuePair<double, NetworkParametersMatrix> pair)
            => new FrequencyParametersPair(pair.Key, pair.Value);
        /// <summary>Creates a <see cref="KeyValuePair{TKey, TValue}"/> from a <see cref="FrequencyParametersPair"/> object.</summary>
        public static implicit operator KeyValuePair<double, NetworkParametersMatrix>(FrequencyParametersPair pair)
            => new KeyValuePair<double, NetworkParametersMatrix>(pair.Frequency_Hz, pair.Parameters);

        /// <summary>
        /// Adds support for the ValueTuple deconstruction syntax for this object.
        /// </summary>
        public void Deconstruct(out double frequency, out NetworkParametersMatrix parameters)
        {
            frequency = Frequency_Hz;
            parameters = Parameters;
        }
    }

    /// <summary>
    /// Represents a collection of frequency dependent network parameters.
    /// </summary>
    public sealed class NetworkParametersCollection : ICollection<FrequencyParametersPair>
    {
        private Dictionary<double, NetworkParametersMatrix> _NetworkParameters = new Dictionary<double, NetworkParametersMatrix>();

        /// <summary>Gets the number of ports of the device that this collection represents.</summary>
        public int NumberOfPorts { get; }

        /// <summary>Gets all frequencies defined in this collection in Hz.</summary>
        public ICollection<double> Frequencies => _NetworkParameters.Keys;
        /// <summary>Gets all the network parameters defined in this collection.</summary>
        public ICollection<NetworkParametersMatrix> NetworkParameters => _NetworkParameters.Values;
        /// <summary>Gets the specific subtype of <see cref="NetworkParametersMatrix"/> represented by this collection.</summary>
        /// <remarks>This collection is often created from a file and so the network parameter matrix type will not be known until after this object is created.</remarks>
        public Type NetworkParameterType { get; }

        /// <summary>
        /// Creates a new empty <see cref="NetworkParametersCollection"/> with the specified number of ports and matrix type.
        /// </summary>
        /// <remarks>External callers should use the static generic <see cref="Create{T}(int)"/> function instead for simplicity.</remarks>
        /// <param name="numPorts">The number of ports of the device this collection represents.</param>
        /// <param name="matrixType">The subtype of <see cref="NetworkParametersMatrix"/> that will be created for each new data point in the collection.</param>
        public NetworkParametersCollection(int numPorts, Type matrixType)
        {
            NumberOfPorts = numPorts;

            if (!matrixType.IsSubclassOf(typeof(NetworkParametersMatrix)) && matrixType != typeof(NetworkParametersMatrix))
            {
                throw new ArgumentException($"Parameter matrix type must derive from {typeof(NetworkParametersMatrix)}.", nameof(matrixType));
            }
            NetworkParameterType = matrixType;
        }
        /// <summary>
        /// Creates a new network parameters collection from a seqeunce of frequency and network parameter matrix pairs.
        /// </summary>
        /// <param name="parameters">A sequence of frequency and network parameter pairs.</param>
        /// <remarks>The number of ports and <see cref="NetworkParametersMatrix"/> subtypes must match for all contained items.</remarks>
        internal NetworkParametersCollection(IEnumerable<FrequencyParametersPair> parameters)
        {
            bool first = true;
            foreach(var pair in parameters)
            {
                if (first)
                {
                    NumberOfPorts = pair.Parameters.NumPorts;
                    NetworkParameterType = pair.Parameters.GetType();
                    first = false;
                }
                this[pair.Frequency_Hz] = pair.Parameters;
            }
        }
        /// <summary>
        /// Creates a new <see cref="NetworkParametersCollection"/> with the specified number of ports and matrix type.
        /// </summary>
        /// <typeparam name="T">The subtype of <see cref="NetworkParametersMatrix"/> that the data in this collection will contain.</typeparam>
        /// <param name="numPorts">The number of ports of the device this collection will represent.</param>
        /// <returns>A new <see cref="NetworkParametersCollection"/> object.</returns>
        public static NetworkParametersCollection Create<T>(int numPorts) where T : NetworkParametersMatrix
            => new NetworkParametersCollection(numPorts, typeof(T));

        /// <summary>
        /// Gets or sets <see cref="NetworkParametersMatrix"/> corresponding to the specified frequency.
        /// </summary>
        /// <param name="frequency">The specified frequency associated with the network data.</param>
        /// <returns>The <see cref="NetworkParametersMatrix"/> measured or derived at <paramref name="frequency"/>.</returns>
        public NetworkParametersMatrix this[double frequency]
        {
            get => _NetworkParameters[frequency];
            set
            {
                if (value.NumPorts != NumberOfPorts)
                {
                    throw new ArgumentException("All network parameter matrices must have the same number of ports.");
                }
                if (value.GetType() != NetworkParameterType)
                {
                    throw new ArgumentException("All network parameter matrices must be of the same network parameter type (scattering, admittance, etc.)");
                }
                _NetworkParameters[frequency] = value;
            }
        }
        /// <summary>
        /// Gets or sets <see cref="NetworkParameter"/> corresponding to the specified frequency and parameter index.
        /// </summary>
        /// <param name="frequency">The specified frequency associated with the network data.</param>
        /// <param name="destinationPort">The destination port of the <see cref="NetworkParameter"/>.</param>
        /// <param name="sourcePort">The source port of the <see cref="NetworkParameter"/>.</param>
        /// <returns>The <see cref="NetworkParameter"/> measured or derived at <paramref name="frequency"/> at index [<paramref name="destinationPort"/>, <paramref name="sourcePort"/>].</returns>
        public NetworkParameter this[double frequency, int destinationPort, int sourcePort]
        {
            get => _NetworkParameters[frequency][destinationPort, sourcePort];
            set
            {
                bool exists = _NetworkParameters.ContainsKey(frequency);
                if (exists)
                {
                    this[frequency][destinationPort, sourcePort] = value;
                }
                else
                {
                    NetworkParametersMatrix matrix = (NetworkParametersMatrix)Activator.CreateInstance(NetworkParameterType, NumberOfPorts);
                    matrix[destinationPort, sourcePort] = value;
                    _NetworkParameters.Add(frequency, matrix);
                }
            }
        }

        /// <summary>Gets the number of <see cref="FrequencyParametersPair"/> objects contained in the collection.</summary>
        public int Count => _NetworkParameters.Count;

        /// <summary>
        /// Determines whether the collection contains the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency to locate in Hz.</param>
        /// <returns>True if the frequency is contained in the collection; false otherwise.</returns>
        public bool ContainsFrequency(double frequency) => _NetworkParameters.ContainsKey(frequency);
        /// <summary>
        /// Adds a new <see cref="NetworkParametersMatrix"/> associated with the measurement or derived frequency to the end of the collection.
        /// </summary>
        /// <param name="frequency">The frequency associated with the <see cref="NetworkParametersMatrix"/>.</param>
        /// <param name="parameters">The network parameters matrix data to add.</param>
        public void Add(double frequency, NetworkParametersMatrix parameters) => this[frequency] = parameters;
        /// <summary>
        /// Removes all frequencies and network parameter matrices from the collection.
        /// </summary>
        public void Clear() => _NetworkParameters.Clear();
        /// <summary>
        /// Attempts to remove the <see cref="NetworkParametersMatrix"/> at the specified frequency from the collection.
        /// </summary>
        /// <param name="frequency">The frequency at which to remove the <see cref="NetworkParametersMatrix"/>.</param>
        /// <returns>True if the element is successfully found and removed; false if not.</returns>
        public bool Remove(double frequency) => _NetworkParameters.Remove(frequency);
        /// <summary>
        /// Gets the <see cref="NetworkParametersMatrix"/> associated with the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency associated with the network parameters matrix to get.</param>
        /// <param name="parameters">The <see cref="NetworkParametersMatrix"/> at <paramref name="frequency"/> if it exists; otherwise will be null.</param>
        /// <returns>True if the <see cref="NetworkParametersMatrix"/> at <paramref name="frequency"/> was found; false otherwise.</returns>
        public bool TryGetValue(double frequency, out NetworkParametersMatrix parameters) => _NetworkParameters.TryGetValue(frequency, out parameters);
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="NetworkParametersCollection"/>.
        /// </summary>
        /// <returns>A sequence of <see cref="FrequencyParametersPair"/> objects representing the network parameters matrix and associated frequency.</returns>
        public IEnumerator<FrequencyParametersPair> GetEnumerator()
        {
            foreach (var pair in _NetworkParameters)
            {
                yield return pair;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Cascades (embeds) the <see cref="NetworkParametersMatrix"/> objects for all common frequencies between subsequently connected ports to render a single composite
        /// <see cref="NetworkParametersCollection"/> defining the relationship between the ports of the first device and the ports of the second device.
        /// </summary>
        /// <param name="next">The <see cref="NetworkParametersCollection"/> to cascade with the current collection.</param>
        /// <returns>A new composite <see cref="NetworkParametersCollection"/> describing the network from the ports of the first device to the ports of the second device across frequency.</returns>
        public NetworkParametersCollection Cascade(NetworkParametersCollection next)
        {
            return Cascade(this, next);
        }
        /// <summary>
        /// Cascades (embeds) a series <see cref="NetworkParametersMatrix"/> objects for all common frequencies between subsequently connected ports to render a single composite
        /// <see cref="NetworkParametersCollection"/> defining the relationship between the ports of the first device and the ports of the final device across frequency.
        /// </summary>
        /// <param name="collections">The array of <see cref="NetworkParametersCollection"/> objects to cascade.</param>
        /// <returns>A new composite <see cref="NetworkParametersCollection"/> describing the network from the ports of the first device to the ports of the final device across frequency.</returns>
        public static NetworkParametersCollection Cascade(params NetworkParametersCollection[] collections)
        {
            var allFrequencies = collections.SelectMany(c => c.Frequencies).Distinct();

            NetworkParametersCollection collection = new NetworkParametersCollection(collections[0].NumberOfPorts, collections[0].NetworkParameterType);

            foreach (var frequency in allFrequencies)
            {
                collection[frequency] = NetworkParametersMatrix.Cascade(collections.Select(c => c[frequency]).ToList());
            }
            return collection;
        }

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
    public static class CollectionUtilities
    {
        /// <summary>
        /// Creates a <see cref="NetworkParametersCollection"/> from a sequence of <see cref="FrequencyParametersPair"/> objects.
        /// </summary>
        /// <param name="data">The network data to fill the collection with.</param>
        /// <returns>A new <see cref="NetworkParametersCollection"/>.</returns>
        public static NetworkParametersCollection ToNetworkParametersCollection(this IEnumerable<FrequencyParametersPair> data)
            => new NetworkParametersCollection(data);
    }
}

