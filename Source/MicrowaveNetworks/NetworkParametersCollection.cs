using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using MathNet.Numerics.Interpolation;
using MicrowaveNetworks.Matrices;
using MicrowaveNetworks.Touchstone.IO;
using MicrowaveNetworks.Internal;
using System.Threading.Tasks;

using MathNet.Numerics;

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
        public void Deconstruct(out double frequency, out TMatrix parameters)
        {
            frequency = Frequency_Hz;
            parameters = Parameters;
        }

        /// <summary>
        /// Converts a <see cref="FrequencyParametersPair{TMatrix}"/> to a generic <see cref="FrequencyParametersPair"/> type.
        /// </summary>
        /// <param name="pair">The pair to convert.</param>
        public static implicit operator FrequencyParametersPair(FrequencyParametersPair<TMatrix> pair) => new FrequencyParametersPair(pair.Frequency_Hz, pair.Parameters);

        /// <summary>
        /// Converts a <see cref="FrequencyParametersPair"/> to a specific <see cref="FrequencyParametersPair{TMatrix}"/> type.
        /// </summary>
        /// <param name="pair">The pair to convert.</param>
        public static explicit operator FrequencyParametersPair<TMatrix>(FrequencyParametersPair pair)
            => new FrequencyParametersPair<TMatrix>(pair.Frequency_Hz, pair.Parameters.ConvertParameterType<TMatrix>());
    }

    /// <summary>
    /// Represents a collection of frequency dependent network parameters.
    /// </summary>
    /// <typeparam name="TMatrix">Specifies the network parameter type contained within this collection.</typeparam>
    public sealed class NetworkParametersCollection<TMatrix> : INetworkParametersCollection where TMatrix : NetworkParametersMatrix
    {
        //private readonly Dictionary<double, TMatrix> networkParameters;
        //private readonly SortedArray<double> frequencies;
        private readonly SortedList<double, TMatrix> networkParameters;

        /// <summary>Gets the number of ports of the device that this collection represents.</summary>
        public int NumberOfPorts { get; }
        /// <summary>Gets all frequencies defined in this collection in Hz.</summary>

#if NET8_0_OR_GREATER
        public IReadOnlyCollection<double> Frequencies => networkParameters.Keys.AsReadOnly();
#else
        public IReadOnlyCollection<double> Frequencies => new ReadOnlyCollection<double>(networkParameters.Keys);
#endif
        /// <summary>Gets all the network parameters defined in this collection.</summary>
#if NET8_0_OR_GREATER
        public IReadOnlyCollection<TMatrix> NetworkParameters => networkParameters.Values.AsReadOnly();
#else
        public IReadOnlyCollection<TMatrix> NetworkParameters => new ReadOnlyCollection<TMatrix>(networkParameters.Values);
#endif
        IReadOnlyCollection<NetworkParametersMatrix> INetworkParametersCollection.NetworkParameters => NetworkParameters;
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
            networkParameters = new SortedList<double, TMatrix>();
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
        /// Creates a new <see cref="NetworkParametersCollection{TMatrix}"/> from an existing <see cref="IList{T}"/> of 
        /// <see cref="FrequencyParametersPair{TMatrix}"/> pairs.
        /// </summary>
        /// <param name="parameters">The list of parameters </param>
        /// <param name="sorted"></param>
        public NetworkParametersCollection(IList<FrequencyParametersPair<TMatrix>> parameters, bool sorted = false)
        {
            var dictionary = parameters.ToDictionary(el => el.Frequency_Hz, el => el.Parameters);
            networkParameters = new SortedList<double, TMatrix>(dictionary);
        }

        /// <summary>
        /// Gets or sets <see cref="NetworkParametersMatrix"/> corresponding to the specified frequency.
        /// </summary>
        /// <param name="frequency">The specified frequency associated with the network data.</param>
        /// <returns>The <see cref="NetworkParametersMatrix"/> measured or derived at <paramref name="frequency"/>.</returns>
        public TMatrix this[double frequency]
        {
            get
            {
                bool exists = networkParameters.TryGetValue(frequency, out TMatrix value);
                if (exists) return value;
                /*else if (Interpolation.Enabled)
                {

                }*/
                else
                {
                    throw new KeyNotFoundException($"No value exists for frequency {frequency}.");
                }
            }
            set
            {
                if (value.NumPorts != NumberOfPorts)
                {
                    throw new ArgumentException("All network parameter matrices must have the same number of ports.");
                }
                networkParameters[frequency] = value;
            }
        }

        /// <summary>
        /// Gets or sets <see cref="NetworkParameter"/> corresponding to the specified frequency and ports.
        /// </summary>
        /// <param name="frequency">The specified frequency associated with the network data.</param>
        /// <param name="destinationPort">The index of the destination port (row). This value is one-indexed.</param>
        /// <param name="sourcePort">The index of the source port (column). This value is one-indexed.</param>
        /// <returns>The <see cref="NetworkParameter"/> at the specified frequency and indices.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if either index is less than one or greather than <see cref="NumberOfPorts"/>.</exception>
        public NetworkParameter this[double frequency, int destinationPort, int sourcePort]
        {
            get => this[frequency][destinationPort, sourcePort];
            set
            {
                bool exists = networkParameters.ContainsKey(frequency);
                if (exists)
                {
                    this[frequency][destinationPort, sourcePort] = value;
                }
                else
                {
                    TMatrix matrix = (TMatrix)Activator.CreateInstance(typeof(TMatrix), NumberOfPorts);
                    matrix[destinationPort, sourcePort] = value;
                    networkParameters.Add(frequency, matrix);
                }
            }
        }
        /// <summary>Finds the <see cref="NetworkParametersMatrix"/> at or nearest to <paramref name="frequency"/>.</summary>
        /// <param name="frequency">The frequency to locate in Hz.</param>
        /// <returns>The <see cref="NetworkParametersMatrix"/> at or nearest to <paramref name="frequency"/>.</returns>
        public TMatrix Nearest(double frequency)
        {
            bool found = networkParameters.TryGetValue(frequency, out TMatrix value);
            if (!found)
            {
                // Keys is cached by the sorted list implementation and not regenerated each time
                int location = networkParameters.Keys.BinarySearch(frequency);

                location = ~location;

                if (location == 0)
                {
                    return networkParameters.Values.First();
                }
                else if (location >= networkParameters.Count)
                {
                    return networkParameters.Values.Last();
                }
                else
                {
                    int predecssorLocation = location - 1;

                    double predecessor = networkParameters.Keys[predecssorLocation];
                    double successor = networkParameters.Keys[location];

                    double preDelta = Math.Abs(predecessor - frequency);
                    double postDelta = Math.Abs(successor - frequency);

                    return preDelta < postDelta ? this[predecessor] : this[successor];
                }
            }
            else return value;
        }
        NetworkParametersMatrix INetworkParametersCollection.Nearest(double frequency) => Nearest(frequency);
        /// <summary>Gets the number of <see cref="FrequencyParametersPair"/> objects contained in the collection.</summary>
        public int Count => networkParameters.Count;

        /// <summary>
        /// Determines whether the collection contains the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency to locate in Hz.</param>
        /// <returns>True if the frequency is contained in the collection; false otherwise.</returns>
        public bool ContainsFrequency(double frequency) => networkParameters.ContainsKey(frequency);
        /// <summary>
        /// Adds a new <see cref="NetworkParametersMatrix"/> associated with the measurement or derived frequency to the end of the collection.
        /// </summary>
        /// <param name="frequency">The frequency associated with the <see cref="NetworkParametersMatrix"/>.</param>
        /// <param name="parameters">The network parameters matrix data to add.</param>
        public void Add(double frequency, TMatrix parameters) => this[frequency] = parameters;
        /// <summary>
        /// Removes all frequencies and network parameter matrices from the collection.
        /// </summary>
        public void Clear()
        {
            networkParameters.Clear();
        }
        /// <summary>
        /// Attempts to remove the <see cref="NetworkParametersMatrix"/> at the specified frequency from the collection.
        /// </summary>
        /// <param name="frequency">The frequency at which to remove the <see cref="NetworkParametersMatrix"/>.</param>
        /// <returns>True if the element is successfully found and removed; false if not.</returns>
        public bool Remove(double frequency)
        {
           return networkParameters.Remove(frequency);
        }
        /// <summary>
        /// Gets the <see cref="NetworkParametersMatrix"/> associated with the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency associated with the network parameters matrix to get.</param>
        /// <param name="parameters">The <see cref="NetworkParametersMatrix"/> at <paramref name="frequency"/> if it exists; otherwise will be null.</param>
        /// <returns>True if the <see cref="NetworkParametersMatrix"/> at <paramref name="frequency"/> was found; false otherwise.</returns>
        public bool TryGetValue(double frequency, out TMatrix parameters) => networkParameters.TryGetValue(frequency, out parameters);
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="NetworkParametersCollection{TMatrix}"/>.
        /// </summary>
        /// <returns>A sequence of <see cref="FrequencyParametersPair"/> objects representing the network parameters matrix and associated frequency.</returns>
        public IEnumerator<FrequencyParametersPair<TMatrix>> GetEnumerator()
        {
            foreach (var pair in networkParameters)
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
            set => this[frequency] = value.ConvertParameterType<TMatrix>();
        }


        bool ICollection<FrequencyParametersPair>.Contains(FrequencyParametersPair item)
        {
            if (networkParameters.TryGetValue(item.Frequency_Hz, out TMatrix value))
            {
                return value.Equals(item.Parameters);
            }
            else return false;
        }

        void ICollection<FrequencyParametersPair>.CopyTo(FrequencyParametersPair[] array, int arrayIndex)
        {
            var arr = Array.ConvertAll(array, f => (KeyValuePair<double, NetworkParametersMatrix>)f);
            ((ICollection<KeyValuePair<double, NetworkParametersMatrix>>)networkParameters).CopyTo(arr, arrayIndex);
        }

        bool ICollection<FrequencyParametersPair>.Remove(FrequencyParametersPair item)
        {
            return Remove(item.Frequency_Hz);
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

    /// <summary>
    /// Contains exension methods for <see cref="IEnumerable{T}"/> types to create a <see cref="NetworkParametersCollection{TMatrix}"/>.
    /// </summary>
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
            => new NetworkParametersCollection<TMatrix>(data.Select(d => (FrequencyParametersPair<TMatrix>)d));
    }
}

