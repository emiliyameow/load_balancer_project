import { FormEvent, useEffect, useMemo, useState } from "react";
import { Activity, AlertTriangle, Gauge, Network, Play, RotateCcw, TimerReset } from "lucide-react";
import { BACKEND_API_BASE_URL, probeBackends, sendBalancerRequest } from "./api/backend";
import { FilterBar } from "./components/FilterBar";
import { MetricStrip } from "./components/MetricStrip";
import { ServerTable } from "./components/ServerTable";
import type { BackendProbe, BalancerResponse, StatusFilter } from "./types/backend";

const refreshIntervalMs = 10000;

const algorithm = {
  name: "MinWeightStrategy"
};

type ColorTheme = "pink" | "dark";

interface RouteStat {
  label: string;
  address: string | null;
  count: number;
  lastLatencyMs: number | null;
  lastStatus: number | null;
  lastAt: Date | null;
}

function getInitialColorTheme(): ColorTheme {
  if (typeof window === "undefined") {
    return "pink";
  }

  const savedTheme = window.localStorage.getItem("balancer-color-theme") ?? window.localStorage.getItem("balancer-theme");
  return savedTheme === "dark" ? "dark" : "pink";
}

function normalize(value: string) {
  return value.trim().toLowerCase();
}

function matchesSearch(server: BackendProbe, query: string) {
  const preparedQuery = normalize(query);

  if (!preparedQuery) {
    return true;
  }

  return [server.serviceName, server.name, server.host, server.address, String(server.port)]
    .map(normalize)
    .some((value) => value.includes(preparedQuery));
}

function matchesStatus(server: BackendProbe, status: StatusFilter) {
  if (status === "all") return true;
  return status === "alive" ? server.status === "alive" : server.status === "down";
}

function sortServers(a: BackendProbe, b: BackendProbe) {
  return (
    a.serviceName.localeCompare(b.serviceName) ||
    a.name.localeCompare(b.name)
  );
}

function formatTime(date: Date | null) {
  return date ? date.toLocaleTimeString() : "Waiting";
}

function average(values: number[]) {
  if (values.length === 0) return null;
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function appendDelay(path: string, delayMs: number) {
  if (delayMs <= 0) return path;

  const separator = path.includes("?") ? "&" : "?";
  return `${path}${separator}delayMs=${delayMs}`;
}

function getRouteLabel(response: BalancerResponse) {
  return response.backendName ?? (response.ok ? "Unknown backend" : "Failed request");
}

export default function App() {
  const [servers, setServers] = useState<BackendProbe[]>([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState<StatusFilter>("all");
  const [selectedService, setSelectedService] = useState("all");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const [probePath, setProbePath] = useState("/test");
  const [delayMs, setDelayMs] = useState(0);
  const [requestCount, setRequestCount] = useState(1);
  const [pendingRequests, setPendingRequests] = useState(0);
  const [responses, setResponses] = useState<BalancerResponse[]>([]);
  const [routeStats, setRouteStats] = useState<Record<string, RouteStat>>({});
  const [colorTheme, setColorTheme] = useState<ColorTheme>(getInitialColorTheme);

  const loadServers = async () => {
    setIsLoading(true);
    try {
      const nextServers = await probeBackends();
      setServers(nextServers);
      setLastUpdated(new Date());
      setError(null);
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : "Failed to load backend registry");
    } finally {
      setIsLoading(false);
    }
  };

  const recordBalancerResponse = (response: BalancerResponse) => {
    setResponses((current) => [response, ...current].slice(0, 18));
    setRouteStats((current) => {
      const label = getRouteLabel(response);
      const previous = current[label];

      return {
        ...current,
        [label]: {
          label,
          address: response.backendAddress ?? previous?.address ?? null,
          count: (previous?.count ?? 0) + 1,
          lastLatencyMs: response.latencyMs,
          lastStatus: response.status,
          lastAt: response.requestedAt
        }
      };
    });
  };

  const sendProbe = async (event?: FormEvent<HTMLFormElement>) => {
    event?.preventDefault();
    const batchSize = requestCount;
    setPendingRequests((current) => current + batchSize);

    const path = appendDelay(probePath, delayMs);
    void loadServers();

    const requests = Array.from({ length: batchSize }, async () => {
      try {
        const response = await sendBalancerRequest(path);
        recordBalancerResponse(response);
        setError(null);
      } catch (caughtError) {
        setError(caughtError instanceof Error ? caughtError.message : "Balancer request failed");
      } finally {
        setPendingRequests((current) => Math.max(0, current - 1));
        void loadServers();
      }
    });

    await Promise.allSettled(requests);
  };

  useEffect(() => {
    void loadServers();
    const refreshId = window.setInterval(() => {
      void loadServers();
    }, refreshIntervalMs);

    return () => window.clearInterval(refreshId);
  }, []);

  useEffect(() => {
    document.documentElement.dataset.theme = "console";
    document.documentElement.dataset.colorTheme = colorTheme;
    window.localStorage.setItem("balancer-color-theme", colorTheme);
  }, [colorTheme]);

  const services = useMemo(
    () => Array.from(new Set(servers.map((server) => server.serviceName))).sort(),
    [servers]
  );

  const filteredServers = useMemo(
    () =>
      servers
        .filter((server) => selectedService === "all" || server.serviceName === selectedService)
        .filter((server) => matchesStatus(server, status))
        .filter((server) => matchesSearch(server, query))
        .sort(sortServers),
    [query, selectedService, servers, status]
  );

  const alive = servers.filter((server) => server.status === "alive").length;
  const down = servers.filter((server) => server.status === "down").length;
  const totalWeight = servers.reduce((sum, server) => sum + (server.weight ?? 0), 0);
  const balancerActive = servers.reduce((sum, server) => sum + server.balancerActiveRequests, 0);
  const averageHealthLatency = average(
    servers.flatMap((server) => (server.latencyMs === null ? [] : [server.latencyMs]))
  );
  const lastResponse = responses[0] ?? null;
  const unhealthyServers = servers.filter((server) => server.status !== "alive" || server.error);

  const routeDistribution = useMemo(() => {
    const configured = servers.map((server) => {
      const stat = routeStats[server.name];

      return {
        label: server.name,
        address: stat?.address ?? server.address,
        count: stat?.count ?? 0,
        lastLatencyMs: stat?.lastLatencyMs ?? null,
        lastStatus: stat?.lastStatus ?? null,
        lastAt: stat?.lastAt ?? null
      };
    });
    const configuredNames = new Set(configured.map((item) => item.label));
    const extra = Object.values(routeStats).filter((item) => !configuredNames.has(item.label));

    return [...configured, ...extra];
  }, [routeStats, servers]);
  const routeTotal = routeDistribution.reduce((sum, item) => sum + item.count, 0);

  const metrics = [
    { label: "Backends", value: servers.length },
    { label: "Alive", value: alive },
    { label: "Down", value: down },
    { label: "Health load", value: totalWeight },
    { label: "LB active", value: balancerActive },
    { label: "Health ms", value: averageHealthLatency === null ? "-" : Math.round(averageHealthLatency) },
    { label: "Client wait", value: pendingRequests }
  ];

  return (
    <main className="app-shell">
      <button
        className="secret-theme-toggle"
        type="button"
        aria-label="Toggle color theme"
        aria-pressed={colorTheme === "dark"}
        title="Theme"
        onClick={() => setColorTheme((currentTheme) => (currentTheme === "dark" ? "pink" : "dark"))}
      />

      <header className="app-header">
        <div>
          <h1>Balancer</h1>
        </div>
      </header>

      {error ? (
        <div className="alert" role="alert">
          <AlertTriangle size={18} aria-hidden="true" />
          <span>{error}</span>
        </div>
      ) : null}

      <section className="control-grid" aria-label="Balancer controls">
        <form className="control-panel probe-panel" onSubmit={sendProbe}>
          <div className="panel-title">
            <Activity size={18} aria-hidden="true" />
            <h2>Proxy probe</h2>
          </div>
          <div className="probe-form">
            <label className="search-field probe-path">
              <span className="sr-only">Request path</span>
              <input value={probePath} onChange={(event) => setProbePath(event.target.value)} />
            </label>
            <label className="search-field small-field">
              <TimerReset size={18} aria-hidden="true" />
              <span className="sr-only">Delay milliseconds</span>
              <input
                min={0}
                max={30000}
                step={100}
                type="number"
                value={delayMs}
                onChange={(event) => setDelayMs(Math.min(30000, Math.max(0, Number(event.target.value) || 0)))}
              />
            </label>
            <select
              className="select-control"
              value={requestCount}
              onChange={(event) => setRequestCount(Number(event.target.value))}
              aria-label="Request count"
            >
              <option value={1}>1 request</option>
              <option value={5}>5 requests</option>
              <option value={10}>10 requests</option>
              <option value={20}>20 requests</option>
            </select>
            <button className="primary-button" type="submit">
              <Play size={18} aria-hidden="true" />
              Send
            </button>
          </div>
          <div className="probe-state" aria-live="polite">
            <span>{pendingRequests} in flight</span>
          </div>
        </form>

        <section className="control-panel algorithm-panel" aria-label="Runtime details">
          <div className="panel-title">
            <Gauge size={18} aria-hidden="true" />
            <h2>Runtime</h2>
          </div>
          <div className="runtime-list">
            <div className="runtime-row">
              <span>Strategy</span>
              <strong>{algorithm.name}</strong>
            </div>
            <div className="runtime-row">
              <span>API</span>
              <strong>{BACKEND_API_BASE_URL}</strong>
            </div>
            <div className="runtime-row">
              <span>Last backend</span>
              <strong>{lastResponse?.backendName ?? "-"}</strong>
            </div>
            <div className="runtime-row">
              <span>LB active</span>
              <strong>{balancerActive}</strong>
            </div>
            <div className="runtime-row">
              <span>Client wait</span>
              <strong>{pendingRequests}</strong>
            </div>
          </div>
        </section>
      </section>

      <MetricStrip metrics={metrics} />

      <section className="insight-grid" aria-label="Traffic and health details">
        <section className="control-panel insight-panel" aria-label="Route distribution">
          <div className="panel-title">
            <Network size={18} aria-hidden="true" />
            <h2>Route mix</h2>
            <button
              className="mini-icon-button"
              type="button"
              aria-label="Reset route mix"
              title="Reset route mix"
              onClick={() => setRouteStats({})}
            >
              <RotateCcw size={15} aria-hidden="true" />
            </button>
          </div>
          {routeDistribution.length > 0 && routeTotal > 0 ? (
            <div className="distribution-list">
              {routeDistribution.map((item) => (
                <div className="distribution-row" key={item.label}>
                  <strong>{item.label}</strong>
                  <span className="mono-token">{item.address ?? "-"}</span>
                  <span className="route-meter" aria-label={`${item.label} ${item.count} requests`}>
                    <i style={{ width: `${routeTotal === 0 ? 0 : (item.count / routeTotal) * 100}%` }} />
                  </span>
                  <b>{item.count}</b>
                  <span className="route-meta">
                    {routeTotal === 0 ? 0 : Math.round((item.count / routeTotal) * 100)}%
                    {item.lastLatencyMs === null ? "" : ` / ${item.lastLatencyMs} ms`}
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <div className="muted-panel">No route samples</div>
          )}
        </section>

        <section className="control-panel insight-panel" aria-label="Health signals">
          <div className="panel-title">
            <AlertTriangle size={18} aria-hidden="true" />
            <h2>Signals</h2>
          </div>
          {unhealthyServers.length > 0 ? (
            <div className="attention-list">
              {unhealthyServers.map((server) => (
                <div className="attention-row" key={`${server.serviceName}:${server.name}`}>
                  <strong>{server.name}</strong>
                  <span>{server.error ?? server.status}</span>
                </div>
              ))}
            </div>
          ) : (
            <div className="muted-panel">All configured backends healthy</div>
          )}
        </section>
      </section>

      {responses.length > 0 ? (
        <section className="response-log" aria-label="Balancer responses">
          {responses.map((response, index) => (
            <article className="response-row" key={`${response.requestedAt.toISOString()}:${index}`}>
              <span
                className={response.ok ? "response-status response-status--ok" : "response-status response-status--fail"}
              >
                {response.status}
              </span>
              <span className="mono-token">{response.path}</span>
              <strong>{response.backendName ?? "-"}</strong>
              <span>{response.backendEffectiveWeight ?? response.backendWeight ?? "-"}</span>
              <strong>{response.latencyMs} ms</strong>
              <span>{response.body || "empty"}</span>
            </article>
          ))}
        </section>
      ) : null}

      <FilterBar
        query={query}
        status={status}
        services={services}
        selectedService={selectedService}
        isLoading={isLoading}
        onQueryChange={setQuery}
        onStatusChange={setStatus}
        onServiceChange={setSelectedService}
        onRefresh={() => void loadServers()}
      />

      <div className="status-line">
        <span>{filteredServers.length} visible</span>
        <span>Updated {formatTime(lastUpdated)}</span>
      </div>

      <ServerTable servers={filteredServers} />
    </main>
  );
}
