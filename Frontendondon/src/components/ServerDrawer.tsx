import { type FormEvent, useEffect, useState } from "react";
import { createBackend, updateBackend } from "../api/backend";
import type { BackendProbe, CreateServerDTO, UpdateServerDTO } from "../types/backend";

interface ServerDrawerProps {
  open: boolean;
  server: BackendProbe | null;
  onClose: () => void;
  onSuccess: () => void;
}

interface FormData {
  serviceName: string;
  name: string;
  address: string;
  host: string;
  port: string;
  weight: string;
}

function isEditMode(server: BackendProbe | null): server is BackendProbe {
  return server !== null;
}

const initialForm: FormData = {
  serviceName: "",
  name: "",
  address: "",
  host: "",
  port: "5001",
  weight: "1"
};

function toCreateDTO(data: FormData): CreateServerDTO {
  return {
    serviceName: data.serviceName.trim(),
    name: data.name.trim(),
    address: data.address.trim(),
    host: data.host.trim(),
    port: Number(data.port),
    weight: Number(data.weight)
  };
}

function toUpdateDTO(data: FormData): UpdateServerDTO {
  return {
    serviceName: data.serviceName.trim(),
    name: data.name.trim(),
    address: data.address.trim() || null,
    host: data.host.trim() || null,
    port: data.port ? Number(data.port) : null,
    weight: data.weight ? Number(data.weight) : null
  };
}

export function ServerDrawer({ open, server, onClose, onSuccess }: ServerDrawerProps) {
  const edit = isEditMode(server);
  const [form, setForm] = useState<FormData>(initialForm);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (open) {
      if (server) {
        setForm({
          serviceName: server.serviceName,
          name: server.name,
          address: server.address,
          host: server.host,
          port: String(server.port),
          weight: String(server.weight ?? "")
        });
      } else {
        setForm(initialForm);
      }
      setError(null);
    }
  }, [open, server]);

  if (!open) return null;

  const handleChange = (field: keyof FormData) => (event: React.ChangeEvent<HTMLInputElement>) => {
    setForm((prev) => ({ ...prev, [field]: event.target.value }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    if (!form.serviceName.trim()) {
      setError("Service name is required");
      return;
    }
    if (!form.name.trim()) {
      setError("Name is required");
      return;
    }

    if (!edit) {
      if (!form.address.trim()) {
        setError("Address is required");
        return;
      }
      if (!form.host.trim()) {
        setError("Host is required");
        return;
      }
      if (!form.port || Number(form.port) < 1 || Number(form.port) > 65535) {
        setError("Port must be between 1 and 65535");
        return;
      }
      if (!form.weight || Number(form.weight) <= 0) {
        setError("Weight must be greater than 0");
        return;
      }
    }

    setSubmitting(true);
    try {
      if (edit) {
        await updateBackend(toUpdateDTO(form));
      } else {
        await createBackend(toCreateDTO(form));
      }
      onSuccess();
      onClose();
    } catch (caughtError) {
      setError(caughtError instanceof Error ? caughtError.message : "Request failed");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="drawer-layer">
      <button className="drawer-scrim" type="button" aria-label="Close" onClick={onClose} />
      <div className="drawer" role="dialog" aria-modal="true" aria-label={edit ? "Edit server" : "Add server"}>
        <div className="drawer-header">
          <h2>{edit ? "Edit server" : "Add server"}</h2>
          <button className="mini-icon-button" type="button" aria-label="Close" onClick={onClose}>✕</button>
        </div>
        <form className="server-form" onSubmit={handleSubmit}>
          <label>
            <span>Service name</span>
            <input
              value={form.serviceName}
              onChange={handleChange("serviceName")}
              disabled={edit}
              required
              placeholder="auth-service"
            />
          </label>
          <label>
            <span>Name</span>
            <input
              value={form.name}
              onChange={handleChange("name")}
              disabled={edit}
              required
              placeholder="backend-1"
            />
          </label>
          <label>
            <span>Address</span>
            <input
              value={form.address}
              onChange={handleChange("address")}
              placeholder="127.0.0.1"
            />
          </label>
          <label>
            <span>Host</span>
            <input
              value={form.host}
              onChange={handleChange("host")}
              placeholder="localhost"
            />
          </label>
          <label>
            <span>Port</span>
            <input
              type="number"
              min={1}
              max={65535}
              value={form.port}
              onChange={handleChange("port")}
              placeholder="5001"
            />
          </label>
          <label>
            <span>Weight</span>
            <input
              type="number"
              min={1}
              value={form.weight}
              onChange={handleChange("weight")}
              placeholder="1"
            />
          </label>
          {error ? <p className="form-error">{error}</p> : null}
          <div className="form-actions">
            <button className="secondary-button" type="button" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
            <button className="primary-button" type="submit" disabled={submitting}>
              {submitting ? "Saving..." : edit ? "Update" : "Create"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
