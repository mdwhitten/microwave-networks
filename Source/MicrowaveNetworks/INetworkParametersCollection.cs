using System;
using System.Collections.Generic;

namespace MicrowaveNetworks
{
    /// <summary>Represents a collection of frequency dependent network parameters. This interface allows Touchstone data to be loaded without knowing the
    /// network parameter type of the file data. <see cref="NetworkParametersCollection{TMatrix}"/> implements this interface with for a specific network
    /// parameter type.
    /// </summary>
    public interface INetworkParametersCollection : ICollection<FrequencyParametersPair>
    {
        /// <summary>
        /// Gets or sets a <see cref="NetworkParametersMatrix"/> corresponding to the specified frequency.
        /// </summary>
        /// <param name="frequency">The specified frequency associated with the network data.</param>
        /// <returns>The <see cref="NetworkParametersMatrix"/> measured or derived at <paramref name="frequency"/>.</returns>
        NetworkParametersMatrix this[double frequency] { get; set; }
        /// <summary>
        /// Gets or sets <see cref="NetworkParameter"/> corresponding to the specified frequency and parameter index.
        /// </summary>
        /// <param name="frequency">The specified frequency associated with the network data.</param>
        /// <param name="destinationPort">The destination port of the <see cref="NetworkParameter"/>.</param>
        /// <param name="sourcePort">The source port of the <see cref="NetworkParameter"/>.</param>
        /// <returns>The <see cref="NetworkParameter"/> measured or derived at <paramref name="frequency"/> at index [<paramref name="destinationPort"/>, <paramref name="sourcePort"/>].</returns>
        NetworkParameter this[double frequency, int destinationPort, int sourcePort] { get; set; }

        /// <summary>Gets all frequencies defined in this collection in Hz.</summary>
        IReadOnlyCollection<double> Frequencies { get; }
        /// <summary>Gets all the network parameters defined in this collection.</summary>
        IReadOnlyCollection<NetworkParametersMatrix> NetworkParameters { get; }
        /// <summary>Gets the specific subtype of <see cref="NetworkParametersMatrix"/> represented by this collection.</summary>
        /// <remarks>This collection is often created from a file and so the network parameter matrix type will not be known until after this object is created.</remarks>
        Type NetworkParameterType { get; }
        /// <summary>Gets the number of ports of the device that this collection represents.</summary>
        int NumberOfPorts { get; }

        /// <summary>
        /// Adds a new <see cref="NetworkParametersMatrix"/> associated with the measurement or derived frequency to the end of the collection.
        /// </summary>
        /// <param name="frequency">The frequency associated with the <see cref="NetworkParametersMatrix"/>.</param>
        /// <param name="parameters">The network parameters matrix data to add.</param>
        void Add(double frequency, NetworkParametersMatrix parameters);
        /// <summary>
        /// Cascades (embeds) the <see cref="NetworkParametersMatrix"/> objects for all common frequencies between subsequently connected ports to render a single composite
        /// <see cref="NetworkParametersCollection{TMatrix}"/> defining the relationship between the ports of the first device and the ports of the second device.
        /// </summary>
        /// <param name="next">The <see cref="INetworkParametersCollection"/> to cascade with the current collection.</param>
        /// <returns>A new composite <see cref="INetworkParametersCollection"/> describing the network from the ports of the first device to the ports of the second device across frequency.</returns>
        INetworkParametersCollection Cascade(INetworkParametersCollection next);
        /// <summary>
        /// Determines whether the collection contains the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency to locate in Hz.</param>
        /// <returns>True if the frequency is contained in the collection; false otherwise.</returns>
        bool ContainsFrequency(double frequency);
        /// <summary>
        /// Attempts to remove the <see cref="NetworkParametersMatrix"/> at the specified frequency from the collection.
        /// </summary>
        /// <param name="frequency">The frequency at which to remove the <see cref="NetworkParametersMatrix"/>.</param>
        /// <returns>True if the element is successfully found and removed; false if not.</returns>
        bool Remove(double frequency);
        /// <summary>
        /// Gets the <see cref="NetworkParametersMatrix"/> associated with the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency associated with the network parameters matrix to get.</param>
        /// <param name="parameters">The <see cref="NetworkParametersMatrix"/> at <paramref name="frequency"/> if it exists; otherwise will be null.</param>
        /// <returns>True if the <see cref="NetworkParametersMatrix"/> at <paramref name="frequency"/> was found; false otherwise.</returns>
        bool TryGetValue(double frequency, out NetworkParametersMatrix parameters);
    }
}