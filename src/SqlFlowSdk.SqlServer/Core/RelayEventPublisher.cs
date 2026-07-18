// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SqlServerFlowSdk;

namespace SqlServerFlowSdk.Core;

/// <summary>
/// Provides an implementation of the IEventPublisher interface that publishes events to an SqlServerFlow event queue.
/// </summary>
/// <remarks>This class is intended for internal use to integrate with the SqlServerFlow event system. It delegates event
/// publishing to an underlying ISqlServerFlow client. Thread safety and error handling depend on the behavior of the provided
/// ISqlServerFlow implementation.</remarks>
public class SqlServerFlowEventPublisher : IEventPublisher
{
    private readonly ISqlFlow _client;

    /// <summary>
    /// Initializes a new instance of the SqlServerFlowEventPublisher class using the specified SqlServerFlow client.
    /// </summary>
    /// <param name="client">The client instance used to communicate with the SqlServerFlow service. Cannot be null.</param>
    public SqlServerFlowEventPublisher(ISqlFlow client)
    {
        _client = client;
    }

    /// <summary>
    /// Asynchronously emits an event with the specified payload to the given queue.
    /// </summary>
    /// <typeparam name="TPayload">The type of the payload to include with the event.</typeparam>
    /// <param name="queue">The name of the queue to which the event will be emitted. Cannot be null or empty.</param>
    /// <param name="eventName">The name of the event to emit. Cannot be null or empty.</param>
    /// <param name="payload">The payload data to include with the event.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous emit operation.</returns>
    public async Task EmitEventAsync<TPayload>(string queue, string eventName, TPayload payload, CancellationToken cancellationToken)
    {
        await _client.EmitEventAsync(new EmitEventOptions { Queue = queue }, eventName, payload, cancellationToken);
    }
}