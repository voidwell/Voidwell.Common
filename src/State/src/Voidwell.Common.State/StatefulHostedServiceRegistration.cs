namespace Voidwell.Common.State;

/// <summary>
/// A stateful hosted service registered with the manager.
/// </summary>
/// <param name="ServiceType">The type the service is resolved by.</param>
internal sealed record StatefulHostedServiceRegistration(Type ServiceType);
