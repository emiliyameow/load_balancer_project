using LoadBalancer.API.Rout;

namespace LoadBalancer.API.BackendManagement;

public sealed record BackendRegistration(
    string ServiceName,
    BackendConfig ServerInfo,
    int InitialWeight);
