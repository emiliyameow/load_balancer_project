using LoadBalancer.API.HealthCheck;

namespace LoadBalancer.API.Balance;

public sealed class BackendLoadTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<string, int> _activeRequests = new(StringComparer.OrdinalIgnoreCase);

    public BackendReservation Reserve(IReadOnlyList<ServerCondition> servers, BalanceAlgorithm balanceAlgorithm)
    {
        lock (_gate)
        {
            var effectiveServers = servers
                .Select(server =>
                {
                    var address = server.ServerInfo.Address;
                    return new ServerCondition
                    {
                        ServerInfo = server.ServerInfo,
                        IsAlive = server.IsAlive,
                        Weight = server.Weight + GetActiveNoLock(address),
                        LatencyMs = server.LatencyMs,
                        CheckedAt = server.CheckedAt,
                        Error = server.Error
                    };
                })
                .ToList();

            var selectedServer = balanceAlgorithm.GetFreeServer(effectiveServers);
            var selectedAddress = selectedServer.ServerInfo.Address;
            var activeAfterReserve = GetActiveNoLock(selectedAddress) + 1;
            _activeRequests[selectedAddress] = activeAfterReserve;

            return new BackendReservation(
                this,
                selectedServer,
                selectedServer.Weight,
                activeAfterReserve);
        }
    }

    public int GetActiveRequests(string address)
    {
        lock (_gate)
        {
            return GetActiveNoLock(address);
        }
    }

    private void Release(string address)
    {
        lock (_gate)
        {
            var next = GetActiveNoLock(address) - 1;
            if (next <= 0)
            {
                _activeRequests.Remove(address);
                return;
            }

            _activeRequests[address] = next;
        }
    }

    private int GetActiveNoLock(string address)
    {
        return _activeRequests.TryGetValue(address, out var activeRequests)
            ? activeRequests
            : 0;
    }

    public sealed class BackendReservation : IDisposable
    {
        private readonly BackendLoadTracker _tracker;
        private readonly string _address;
        private bool _isDisposed;

        public BackendReservation(
            BackendLoadTracker tracker,
            ServerCondition server,
            int effectiveWeight,
            int activeRequests)
        {
            _tracker = tracker;
            Server = server;
            EffectiveWeight = effectiveWeight;
            ActiveRequests = activeRequests;
            _address = server.ServerInfo.Address;
        }

        public ServerCondition Server { get; }
        public int EffectiveWeight { get; }
        public int ActiveRequests { get; }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _tracker.Release(_address);
            _isDisposed = true;
        }
    }
}
