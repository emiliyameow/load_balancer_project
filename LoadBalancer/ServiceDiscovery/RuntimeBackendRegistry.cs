using LoadBalancer.API.BackendManagement;
using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.Rout;
using Microsoft.Extensions.Options;

namespace LoadBalancer.API.ServiceDiscovery;

public class RuntimeBackendRegistry : IServiceRegistry
{
    private const string DefaultServiceName = "users-service";
    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, BackendRegistration>> _services =
        new(StringComparer.OrdinalIgnoreCase);

    public RuntimeBackendRegistry(IOptionsMonitor<Settings> settings)
    {
        foreach (var backend in settings.CurrentValue.Backends)
        {
            AddOrReplace(DefaultServiceName, backend, initialWeight: 1);
        }
    }

    public Task<Dictionary<string, List<ServerCondition>>> GetServicesAsync(CancellationToken ctsToken = default)
    {
        ctsToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var snapshot = new Dictionary<string, List<ServerCondition>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (serviceName, serviceBackends) in _services)
            {
                snapshot[serviceName] = serviceBackends.Values
                    .Select(registration => new ServerCondition
                    {
                        ServerInfo = CopyConfig(registration.ServerInfo),
                        IsAlive = true,
                        Weight = registration.InitialWeight
                    })
                    .ToList();
            }

            return Task.FromResult(snapshot);
        }
    }

    public IReadOnlyList<BackendRegistration> GetRegisteredBackends()
    {
        lock (_gate)
        {
            return _services.Values
                .SelectMany(service => service.Values)
                .Select(CopyRegistration)
                .OrderBy(registration => registration.ServiceName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(registration => registration.ServerInfo.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public bool TryAdd(
        string serviceName,
        BackendConfig serverInfo,
        int initialWeight,
        out BackendRegistration registration)
    {
        lock (_gate)
        {
            var serviceBackends = GetOrCreateService(serviceName);

            if (serviceBackends.ContainsKey(serverInfo.Name))
            {
                registration = serviceBackends[serverInfo.Name];
                return false;
            }

            registration = new BackendRegistration(serviceName, CopyConfig(serverInfo), initialWeight);
            serviceBackends[serverInfo.Name] = registration;
            return true;
        }
    }

    public bool TryUpdate(
        string serviceName,
        string name,
        string? host,
        int? port,
        int? weight,
        out BackendRegistration? previous,
        out BackendRegistration? updated)
    {
        lock (_gate)
        {
            previous = null;
            updated = null;

            if (!_services.TryGetValue(serviceName, out var serviceBackends) ||
                !serviceBackends.TryGetValue(name, out var existing))
            {
                return false;
            }

            var nextServerInfo = CopyConfig(existing.ServerInfo);

            if (host is not null)
                nextServerInfo.Host = host;

            if (port.HasValue)
                nextServerInfo.Port = port.Value;

            previous = CopyRegistration(existing);
            updated = new BackendRegistration(
                serviceName,
                nextServerInfo,
                weight ?? existing.InitialWeight);

            serviceBackends[name] = updated;
            return true;
        }
    }

    public bool TryRemove(
        string serviceName,
        string name,
        out BackendRegistration? registration)
    {
        lock (_gate)
        {
            registration = null;

            if (!_services.TryGetValue(serviceName, out var serviceBackends) ||
                !serviceBackends.TryGetValue(name, out var existing))
            {
                return false;
            }

            registration = CopyRegistration(existing);
            serviceBackends.Remove(name);

            if (serviceBackends.Count == 0)
                _services.Remove(serviceName);

            return true;
        }
    }

    private void AddOrReplace(string serviceName, BackendConfig serverInfo, int initialWeight)
    {
        var serviceBackends = GetOrCreateService(serviceName);
        var registration = new BackendRegistration(serviceName, CopyConfig(serverInfo), initialWeight);
        serviceBackends[serverInfo.Name] = registration;
    }

    private Dictionary<string, BackendRegistration> GetOrCreateService(string serviceName)
    {
        if (!_services.TryGetValue(serviceName, out var serviceBackends))
        {
            serviceBackends = new Dictionary<string, BackendRegistration>(StringComparer.OrdinalIgnoreCase);
            _services[serviceName] = serviceBackends;
        }

        return serviceBackends;
    }

    private static BackendRegistration CopyRegistration(BackendRegistration registration)
    {
        return registration with
        {
            ServerInfo = CopyConfig(registration.ServerInfo)
        };
    }

    private static BackendConfig CopyConfig(BackendConfig source)
    {
        return new BackendConfig
        {
            Name = source.Name,
            Host = source.Host,
            Port = source.Port
        };
    }
}
