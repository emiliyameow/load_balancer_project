import { Pencil } from "lucide-react";
import type { BackendProbe } from "../types/backend";
import { StatusBadge } from "./StatusBadge";

interface ServerTableProps {
  servers: BackendProbe[];
  onEdit?: (server: BackendProbe) => void;
}

function formatCheckedAt(date: Date | null) {
  return date ? date.toLocaleTimeString() : "Never";
}

export function ServerTable({ servers, onEdit }: ServerTableProps) {
  if (servers.length === 0) {
    return (
      <section className="empty-state">
        <strong>No servers</strong>
        <span>Change filters or refresh the configured targets.</span>
      </section>
    );
  }

  return (
    <section className="table-shell" aria-label="Configured backend servers">
      <table>
        <thead>
          <tr>
            <th>Service</th>
            <th>Name</th>
            <th>Address</th>
            <th>Health</th>
            <th>Health load</th>
            <th>LB active</th>
            <th>Effective</th>
            <th>Health ms</th>
            <th>Checked</th>
            <th>Signal</th>
            {onEdit ? <th>Actions</th> : null}
          </tr>
        </thead>
        <tbody>
          {servers.map((server) => (
            <tr key={`${server.serviceName}:${server.name}`}>
              <td>
                <span className="mono-token">{server.serviceName}</span>
              </td>
              <td>{server.name}</td>
              <td>
                <span className="endpoint">{server.address}</span>
              </td>
              <td>
                <StatusBadge status={server.status} />
              </td>
              <td>
                <strong>{server.weight ?? "-"}</strong>
              </td>
              <td>
                <strong>{server.balancerActiveRequests}</strong>
              </td>
              <td>
                <strong>{server.effectiveWeight ?? "-"}</strong>
              </td>
              <td>{server.latencyMs === null ? "-" : `${server.latencyMs} ms`}</td>
              <td>{formatCheckedAt(server.checkedAt)}</td>
              <td className={server.error ? "signal-cell signal-cell--warn" : "signal-cell"}>
                {server.error ?? "OK"}
              </td>
              {onEdit ? (
                <td>
                  <button
                    className="icon-button"
                    type="button"
                    aria-label={`Edit ${server.name}`}
                    title="Edit server"
                    onClick={() => onEdit(server)}
                  >
                    <Pencil size={15} aria-hidden="true" />
                  </button>
                </td>
              ) : null}
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
