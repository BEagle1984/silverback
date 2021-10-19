namespace Silverback.Messaging.Outbound.EndpointResolvers;

/// <summary>
///     Dynamically resolves the target endpoint (e.g. the target topic and partition) for each message being produced.
/// </summary>
public interface IDynamicProducerEndpointResolver : IProducerEndpointResolver, IProducerEndpointSerializer
{
}