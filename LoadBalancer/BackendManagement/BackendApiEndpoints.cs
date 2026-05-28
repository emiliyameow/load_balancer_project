using System.Collections.Immutable;
using LoadBalancer.API.Balance;
using LoadBalancer.API.HealthCheck;
using LoadBalancer.API.Rout;
using LoadBalancer.API.ServiceCache;
using LoadBalancer.API.ServiceDiscovery;

namespace LoadBalancer.API.BackendManagement;

public static class BackendApiEndpoints
{
    public static IEndpointRouteBuilder MapBackendApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/backend");

        group.MapGet("", (RuntimeBackendRegistry registry, HealthCache healthCache, BackendLoadTracker loadTracker) =>
        {
            return Results.Ok(BuildServerDtos(registry, healthCache, loadTracker));
        });

        group.MapPost("", async (
            CreateServerDto request,
            RuntimeBackendRegistry registry,
            ServiceCacheHandler cache,
            HealthCache healthCache,
            BackendLoadTracker loadTracker) =>
        {
            var input = ValidateCreate(request);
            if (input.Error is not null)
                return Results.BadRequest(input.Error);

            var serverInfo = new BackendConfig
            {
                Name = input.Name,
                Host = input.Host ?? string.Empty,
                Port = input.Port!.Value
            };

            if (!registry.TryAdd(input.ServiceName, serverInfo, input.Weight!.Value, out var registration))
                return Results.Conflict("Server already exists");

            await SyncServiceCacheAsync(registry, cache, input.ServiceName);
            WarmHealthCache(healthCache, registration);

            return Results.Created("/api/backend", ToDto(registration, healthCache, loadTracker));
        });

        group.MapPatch("/update", async (
            UpdateServerDto request,
            RuntimeBackendRegistry registry,
            ServiceCacheHandler cache,
            HealthCache healthCache) =>
        {
            var input = ValidateUpdate(request);
            if (input.Error is not null)
                return Results.BadRequest(input.Error);

            if (!registry.TryUpdate(
                    input.ServiceName,
                    input.Name,
                    input.Host,
                    input.Port,
                    input.Weight,
                    out var previous,
                    out var updated) ||
                updated is null)
            {
                return Results.NotFound("Server not found");
            }

            if (previous is not null &&
                !string.Equals(previous.ServerInfo.Address, updated.ServerInfo.Address, StringComparison.OrdinalIgnoreCase))
            {
                healthCache.Remove(previous.ServerInfo.Address);
            }

            await SyncServiceCacheAsync(registry, cache, input.ServiceName);
            WarmHealthCache(healthCache, updated);

            return Results.NoContent();
        });

        return app;
    }

    private static List<ServerDto> BuildServerDtos(
        RuntimeBackendRegistry registry,
        HealthCache healthCache,
        BackendLoadTracker loadTracker)
    {
        return registry.GetRegisteredBackends()
            .Select(registration => ToDto(registration, healthCache, loadTracker))
            .ToList();
    }

    private static ServerDto ToDto(
        BackendRegistration registration,
        HealthCache healthCache,
        BackendLoadTracker loadTracker)
    {
        var address = registration.ServerInfo.Address;
        var hasHealth = healthCache.TryGet(address, out var health);
        var activeRequests = loadTracker.GetActiveRequests(address);
        var healthWeight = hasHealth ? health.Weight : registration.InitialWeight;
        var isAlive = hasHealth && health.IsAlive;
        var error = hasHealth
            ? health.Error
            : "Health check pending";

        if (hasHealth && !health.IsAlive && string.IsNullOrWhiteSpace(error))
            error = "Backend is not healthy";

        return new ServerDto(
            address,
            registration.ServerInfo.Port,
            isAlive,
            registration.ServerInfo.Name,
            registration.ServerInfo.Host,
            registration.ServiceName,
            healthWeight,
            activeRequests,
            healthWeight + activeRequests,
            hasHealth ? health.LatencyMs : null,
            hasHealth ? health.CheckedAt : null,
            error);
    }

    private static async Task SyncServiceCacheAsync(
        RuntimeBackendRegistry registry,
        ServiceCacheHandler cache,
        string serviceName)
    {
        var services = await registry.GetServicesAsync();
        if (services.TryGetValue(serviceName, out var serviceBackends))
            cache.AddOrUpdateService(serviceName, serviceBackends.ToImmutableList());
    }

    private static void WarmHealthCache(HealthCache healthCache, BackendRegistration registration)
    {
        healthCache.Upsert(
            registration.ServerInfo.Address,
            new ServerHealth(
                true,
                registration.InitialWeight,
                0,
                DateTimeOffset.UtcNow,
                null));
    }

    private static ValidatedBackendInput ValidateCreate(CreateServerDto request)
    {
        var errors = new List<string>();
        var parsedAddress = ParseAddress(request.Address);
        var serviceName = request.ServiceName.Trim();
        var name = request.Name.Trim();
        var host = ResolveHost(request.Host, parsedAddress.Host);
        var port = request.Port > 0 ? request.Port : parsedAddress.Port;

        if (string.IsNullOrWhiteSpace(serviceName))
            errors.Add("ServiceName is required");

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name is required");

        if (request.Address is not null && string.IsNullOrWhiteSpace(request.Address))
            errors.Add("Address cannot be empty");

        if (string.IsNullOrWhiteSpace(host))
            errors.Add("Host cannot be empty");

        if (!IsValidPort(port))
            errors.Add("Invalid port");

        if (request.Weight <= 0)
            errors.Add("Weight must be greater than 0");

        return errors.Count > 0
            ? ValidatedBackendInput.Failed(errors)
            : new ValidatedBackendInput(serviceName, name, host!, port, request.Weight, null);
    }

    private static ValidatedBackendInput ValidateUpdate(UpdateServerDto request)
    {
        var errors = new List<string>();
        var parsedAddress = ParseAddress(request.Address);
        var serviceName = request.ServiceName.Trim();
        var name = request.Name.Trim();
        var host = request.Host is null ? parsedAddress.Host : request.Host.Trim();
        var port = request.Port ?? parsedAddress.Port;

        if (string.IsNullOrWhiteSpace(serviceName))
            errors.Add("ServiceName is required");

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Name is required");

        if (request.Address is not null && string.IsNullOrWhiteSpace(request.Address))
            errors.Add("Address cannot be empty");

        if (request.Host is not null && string.IsNullOrWhiteSpace(request.Host))
            errors.Add("Host cannot be empty");

        if (port.HasValue && !IsValidPort(port))
            errors.Add("Invalid port");

        if (request.Weight.HasValue && request.Weight <= 0)
            errors.Add("Weight must be greater than 0");

        return errors.Count > 0
            ? ValidatedBackendInput.Failed(errors)
            : new ValidatedBackendInput(serviceName, name, host, port, request.Weight, null);
    }

    private static string? ResolveHost(string? host, string? parsedAddressHost)
    {
        if (!string.IsNullOrWhiteSpace(host))
            return host.Trim();

        return parsedAddressHost;
    }

    private static ParsedAddress ParseAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return new ParsedAddress(null, null);

        var trimmed = address.Trim();
        var candidate = trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"http://{trimmed}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return new ParsedAddress(trimmed.TrimEnd('/'), null);

        int? port = uri.IsDefaultPort ? null : uri.Port;
        return new ParsedAddress(uri.Host, port);
    }

    private static bool IsValidPort(int? port)
    {
        return port is >= 1 and <= 65535;
    }

    private sealed record ParsedAddress(string? Host, int? Port);

    private sealed record ValidatedBackendInput(
        string ServiceName,
        string Name,
        string? Host,
        int? Port,
        int? Weight,
        string? Error)
    {
        public static ValidatedBackendInput Failed(IEnumerable<string> errors)
        {
            return new ValidatedBackendInput(
                string.Empty,
                string.Empty,
                null,
                null,
                null,
                string.Join("; ", errors));
        }
    }
}
