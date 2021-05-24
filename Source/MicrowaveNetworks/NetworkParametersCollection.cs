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
    /// Contains a network parameter matrix and the measurement frequency associated with the matrix.
    /// </summary>
    /// <typeparam name="TMatrix">Specifies the network parameter type defined by this matrix.</typeparam>
    public readonly struct FrequencyParametersPair<TMatrix> where TMatrix : NetworkParametersMatrix
    {
        /// <summary>Gets the measurement or derived frequency in Hz.</summary>
        public double Frequency_Hz { get; }
        /// <summary>Gets the <see cref="NetworkParametersMatrix"/> measured or derived at <see cref="Frequency_Hz"/>.</summary>
        public TMatrix Parameters { get; }

        /// <summary>Creates a new <see cref="FrequencyParametersPair"/> associated a network parameters matrix and the measurement/derived frequency.</summary>
        /// <param name="frequency">The frequency in Hz.</param>
        /// <param name="parameters">The network parameters matrix.</param>
        public FrequencyParametersPair(double frequency, TMatrix parameters)
        {
            Frequency_Hz = frequency;
            Parameters = parameters;
        }

        /// <summary>Creates a <see cref="FrequencyParametersPair"/> from a <see cref="KeyValuePair{TKey, TValue}"/> object.</summary>
        public static implicit operator FrequencyParametersPair<TMatrix>(KeyValuePair<double, TMatrix> pair)
            => new FrequencyParametersPair<TMatrix>(pair.Key, pair.Value);
        /// <summary>Creates a <see cref="KeyValuePair{TKey, TValue}"/> from a <see cref="FrequencyParametersPair"/> object.</summary>
        public static implicit operator KeyValuePair<double, TMatrix>(FrequencyParametersPair<TMatrix> pair)
            => new KeyValuePair<double, TMatrix>(pair.Frequency_Hz, pair.Parameters);

        /// <summary>
        /// Adds support for the ValueTuple deconstruction syntax for this object.
        /// </summary>
        public void Deconstruct(out double frequency, out NetworkParametersMatrix parameters)
        {
            frequency = Frequency_Hz;
            parameters = Parameters;
        }

        public static implicit operator FrequencyParametersPair(FrequencyParametersPair<TMatrix> pair) => new FrequencyParametersPair(pair.Frequency_Hz, pair.Parameters);
        public static explicit operator FrequencyParametersPair<TMatrix>(FrequencyParametersPair pair)
            => new FrequencyParametersPair<TMatrix>(pair.Frequency_Hz, pair.Parameters.ConvertParameterType<TMatrix>());
    }

    /// <summary>
    /// Represents a collection of frequency dependent network parameters.
    /// </summary>
    /// <typeparam name="TMatrix">Specifies the network parameter type contained within this collection.</typeparam>
    public class NetworkParametersCollection<TMatrix> : INetworkParametersCollection where TMatrix : NetworkParametersMatrix
    {
        private Dictionary<double, TMatrix> _NetworkParameters = new Dictionary<double, TMatrix>();

        /// <summary>Gets the number of ports of the device that this collection represents.</summary>
        public int NumberOfPorts { get; }

        /// <summary>Gets all frequencies defined in this collection in Hz.</summary>
        public ICollection<double> Frequencies => _NetworkParameters.Keys;
        /// <summary>Gets all the network parameters defined in this collection.</summary>
        public ICollection<TMatrix> NetworkParameters => _NetworkParameters.Values;
        ICollection<NetworkParametersMatrix> INetworkParametersCollection.NetworkParameters => (ICollection<NetworkParametersMatrix>)NetworkParameters;
        /// <summary>Gets the specific subtype of <see cref="NetworkParametersMatrix"/> represented by this collection.</summary>
        /// <remarks>This collection is often created from a file and so the network parameter matrix type will not be known until after this object is created.</remarks>
        Type INetworkParametersCollection.NetworkParameterType => typeof(TMatrix);

        /// <summary>
        /// Creates a new empty <see cref="NetworkParametersCollection{TMatrix}"/> with the specified number of ports and matrix type.
        /// </summary>
        /// <param name="numPorts">The number of ports of the device this collection represents.</param>
        public NetworkParametersCollection(int numPorts)
        {
            NumberOfPorts = numPorts;
        }
        /// <summary>
        /// Creates a new network parameters collection from a seqeunce of frequency and network parameter matrix pairs.
        /// </summary>
        /// <param name="parameters">A sequence of frequency and network parameter pairs.</param>
        /// <remarks>The number of ports and <see cref="NetworkParametersMatrix"/> subtypes must match for all contained items.</remarks>
        internal NetworkParametersCollection(IEnumerable<FrequencyParametersPair<TMatrix>> parameters)
        {
            bool first = true;
            foreach (var pair in parameters)
            {
                if (first)
                {
                    NumberOfPorts = pair.Parameters.NumPorts;
                    first = false;
                }
                this[pair.Frequency_Hz] = pair.Parameters;
            }
        }
        /// <summary>
        /// Gets or sets a <see cref="NetworkParametersMatrix"/> corresponding to the specified frequency.
        /// </summary>
        /// <param name="frequency">The specified frequency associated with the network data.</param>
        /// <returns>The <see cref="NetworkParametersMatrix"/> measured or derived at <paramref name="frequency"/>.</returns>
        public TMatrix this[double frequency]
        {
            get => _NetworkParameters[frequency];
            set => _NetworkParameters[frequency] = value;
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
                    TMatrix matrix = (TMatrix)Activator.CreateInstance(typeof(TMatrix), NumberOfPorts);
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
        public void Add(double frequency, TMatrix parameters) => this[frequency] = parameters;
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
        public bool TryGetValue(double frequency, out TMatrix parameters) => _NetworkParameters.TryGetValue(frequency, out parameters);
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="NetworkParametersCollection{TMatrix}"/>.
        /// </summary>
        /// <returns>A sequence of <see cref="FrequencyParametersPair"/> objects representing the network parameters matrix and associated frequency.</returns>
        public IEnumerator<FrequencyParametersPair<TMatrix>> GetEnumerator()
        {
            foreach (var pair in _NetworkParameters)
            {
                yield return pair;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Cascades (embeds) the <see cref="NetworkParametersMatrix"/> objects for all common frequencies between subsequently connected ports to render a single composite
        /// <see cref="NetworkParametersCollection{TMatrix}"/> defining the relationship between the ports of the first device and the ports of the second device.
        /// </summary>
        /// <param name="next">The <see cref="NetworkParametersCollection{TMatrix}"/> to cascade with the current collection.</param>
        /// <returns>A new composite <see cref="NetworkParametersCollection{TMatrix}"/> describing the network from the ports of the first device to the ports of the second device across frequency.</returns>
        public NetworkParametersCollection<TMatrix> Cascade(INetworkParametersCollection next)
        {
            return Cascade<TMatrix>(this, next);
        }
        /// <summary>
        /// Cascades (embeds) a series <see cref="NetworkParametersMatrix"/> objects for all common frequencies between subsequently connected ports to render a single composite
        /// <see cref="NetworkParametersCollection{TMatrix}"/> defining the relationship between the ports of the first device and the ports of the final device across frequency.
        /// </summary>
        /// <param name="collections">The array of <see cref="NetworkParametersCollection{TMatrix}"/> objects to cascade.</param>
        /// <returns>A new composite <see cref="NetworkParametersCollection{TMatrix}"/> describing the network from the ports of the first device to the ports of the final device across frequency.</returns>
        public static NetworkParametersCollection<T> Cascade<T>(params INetworkParametersCollection[] collections) where T : NetworkParametersMatrix
        {
            var allFrequencies = collections.SelectMany(c => c.Frequencies).Distinct();

            NetworkParametersCollection<T> collection = new NetworkParametersCollection<T>(collections[0].NumberOfPorts);

            foreach (var frequency in allFrequencies)
            {
                collection[frequency] = (T)NetworkParametersMatrix.Cascade(collections.Select(c => c[frequency]).ToList());
            }
            return collection;
        }

        #region Explicit ICollection Implementations
        bool ICollection<FrequencyParametersPair>.IsReadOnly => false;


        NetworkParametersMatrix INetworkParametersCollection.this[double frequency]
        {
            get => this[frequency];
            set => value.ConvertParameterType<TMatrix>();
        }


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

        void INetworkParametersCollection.Add(double frequency, NetworkParametersMatrix parameters)
            => Add(frequency, parameters.ConvertParameterType<TMatrix>());

        INetworkParametersCollection INetworkParametersCollection.Cascade(INetworkParametersCollection next)
        {
            throw new NotImplementedException();
        }

        bool INetworkParametersCollection.TryGetValue(double frequency, out NetworkParametersMatrix parameters)
        {
            bool found = TryGetValue(frequency, out TMatrix tParameters);
            parameters = tParameters;
            return found;
        }

        void ICollection<FrequencyParametersPair>.Add(FrequencyParametersPair item) => Add(item.Frequency_Hz, item.Parameters.ConvertParameterType<TMatrix>());

        IEnumerator<FrequencyParametersPair> IEnumerable<FrequencyParametersPair>.GetEnumerator()
        {
            // Have to explicitly execute the generic enumerator as below because the implicit cast
            // to the non-generic FrequencyParametersPair can't be done by casting the IEnumerator object
            using var enumer = GetEnumerator();
            while (enumer.MoveNext())
            {
                yield return enumer.Current;
            }
        }
        #endregion

    }
    public static class CollectionUtilities
    {
        /// <summary>
        /// Creates a <see cref="NetworkParametersCollection{TMatrix}"/> from a sequence of <see cref="FrequencyParametersPair"/> objects.
        /// </summary>
        /// <param name="data">The network data to fill the collection with.</param>
        /// <returns>A new <see cref="NetworkParametersCollection{TMatrix}"/>.</returns>
        public static NetworkParametersCollection<TMatrix> ToNetworkParametersCollection<TMatrix>(this IEnumerable<FrequencyParametersPair<TMatrix>> data) where TMatrix : NetworkParametersMatrix
            => new NetworkParametersCollection<TMatrix>(data);

        /// <summary>
        /// Creates a <see cref="NetworkParametersCollection{TMatrix}"/> from a sequence of <see cref="FrequencyParametersPair"/> objects.
        /// </summary>
        /// <param name="data">The network data to fill the collection with.</param>
        /// <returns>A new <see cref="NetworkParametersCollection{TMatrix}"/>.</returns>
        public static NetworkParametersCollection<TMatrix> ToNetworkParametersCollection<TMatrix>(this IEnumerable<FrequencyParametersPair> data) where TMatrix : NetworkParametersMatrix
            => new NetworkParametersCollection<TMatrix>(data.Select( d => (FrequencyParametersPair<TMatrix>)d));
    }
}

