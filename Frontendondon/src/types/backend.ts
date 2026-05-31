export type HealthStatus = "unknown" | "alive" | "down";

export interface ServerDTO {
  address: string;
  port: number;
  isAlive: boolean;
  name: string;
  host: string;
  serviceName: string;
  weight: number;
  balancerActiveRequests: number;
  effectiveWeight: number;
  latencyMs: number | null;
  checkedAt: string | null;
  error: string | null;
}

export interface CreateServerDTO {
  serviceName: string;
  address: string;
  port: number;
  name: string;
  host: string;
  weight: number;
}

export interface UpdateServerDTO {
  serviceName: string;
  name: string;
  address?: string | null;
  host?: string | null;
  port?: number | null;
  weight?: number | null;
}

export interface BackendProbe {
  address: string;
  name: string;
  serviceName: string;
  host: string;
  port: number;
  status: HealthStatus;
  weight: number | null;
  balancerActiveRequests: number;
  effectiveWeight: number | null;
  latencyMs: number | null;
  checkedAt: Date | null;
  error: string | null;
}

export interface BalancerResponse {
  path: string;
  status: number;
  ok: boolean;
  body: string;
  backendName: string | null;
  backendAddress: string | null;
  backendWeight: number | null;
  backendActiveRequests: number | null;
  backendEffectiveWeight: number | null;
  latencyMs: number;
  requestedAt: Date;
}

export type StatusFilter = "all" | "alive" | "down";
