import type { BackendProbe, BalancerResponse, ServerDTO, CreateServerDTO, UpdateServerDTO } from "../types/backend";

export const BALANCER_PROXY_BASE_URL = "/lb";
export const BACKEND_API_BASE_URL = "/api/backend";

function normalizePath(path: string) {
  const trimmed = path.trim();
  if (!trimmed) return "/";
  return trimmed.startsWith("/") ? trimmed : `/${trimmed}`;
}

function parseHeaderNumber(value: string | null) {
  if (value === null) return null;

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

async function fetchText(url: string): Promise<{ status: number; ok: boolean; body: string; headers: Headers }> {
  const response = await fetch(url, {
    cache: "no-store"
  });

  return {
    status: response.status,
    ok: response.ok,
    body: await response.text(),
    headers: response.headers
  };
}

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url, {
    cache: "no-store",
    headers: {
      Accept: "application/json"
    }
  });

  if (!response.ok) {
    throw new Error(`${response.status} ${await response.text()}`);
  }

  return response.json() as Promise<T>;
}

export async function probeBackends(): Promise<BackendProbe[]> {
  const servers = await fetchJson<ServerDTO[]>(BACKEND_API_BASE_URL);

  return servers.map((server) => ({
    address: server.address,
    name: server.name,
    serviceName: server.serviceName,
    host: server.host,
    port: server.port,
    status: server.isAlive ? "alive" : "down",
    weight: server.weight,
    balancerActiveRequests: server.balancerActiveRequests,
    effectiveWeight: server.effectiveWeight,
    latencyMs: server.latencyMs,
    checkedAt: server.checkedAt ? new Date(server.checkedAt) : null,
    error: server.error
  }));
}

export async function sendBalancerRequest(path: string): Promise<BalancerResponse> {
  const normalizedPath = normalizePath(path);
  const startedAt = performance.now();
  const response = await fetchText(`${BALANCER_PROXY_BASE_URL}${normalizedPath}`);

  return {
    path: normalizedPath,
    status: response.status,
    ok: response.ok,
    body: response.body,
    backendName: response.headers.get("x-balancer-backend-name"),
    backendAddress: response.headers.get("x-balancer-backend-address"),
    backendWeight: parseHeaderNumber(response.headers.get("x-balancer-backend-weight")),
    backendActiveRequests: parseHeaderNumber(response.headers.get("x-balancer-backend-active")),
    backendEffectiveWeight: parseHeaderNumber(response.headers.get("x-balancer-backend-effective-weight")),
    latencyMs: Math.round(performance.now() - startedAt),
    requestedAt: new Date()
  };
}

export async function createBackend(data: CreateServerDTO): Promise<ServerDTO> {
  const response = await fetch(BACKEND_API_BASE_URL, {
    method: "POST",
    cache: "no-store",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json"
    },
    body: JSON.stringify(data)
  });

  if (!response.ok) {
    throw new Error(`${response.status} ${await response.text()}`);
  }

  return response.json() as Promise<ServerDTO>;
}

export async function updateBackend(data: UpdateServerDTO): Promise<void> {
  const response = await fetch(`${BACKEND_API_BASE_URL}/update`, {
    method: "PATCH",
    cache: "no-store",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(data)
  });

  if (response.status === 404) {
    throw new Error("404 Server not found");
  }

  if (!response.ok) {
    throw new Error(`${response.status} ${await response.text()}`);
  }
}

export async function deleteBackend(serviceName: string, name: string): Promise<void> {
  const response = await fetch(
    `${BACKEND_API_BASE_URL}/${encodeURIComponent(serviceName)}/${encodeURIComponent(name)}`,
    {
      method: "DELETE",
      cache: "no-store"
    }
  );

  if (response.status === 404) {
    throw new Error("404 Server not found");
  }

  if (!response.ok) {
    throw new Error(`${response.status} ${await response.text()}`);
  }
}
