import { Plus, RefreshCw, Search } from "lucide-react";
import type { StatusFilter } from "../types/backend";

interface FilterBarProps {
  query: string;
  status: StatusFilter;
  services: string[];
  selectedService: string;
  isLoading: boolean;
  onQueryChange: (query: string) => void;
  onStatusChange: (status: StatusFilter) => void;
  onServiceChange: (service: string) => void;
  onRefresh: () => void;
  onAdd?: () => void;
}

const statuses: Array<{ label: string; value: StatusFilter }> = [
  { label: "All", value: "all" },
  { label: "Alive", value: "alive" },
  { label: "Down", value: "down" }
];

export function FilterBar({
  query,
  status,
  services,
  selectedService,
  isLoading,
  onQueryChange,
  onStatusChange,
  onServiceChange,
  onRefresh,
  onAdd
}: FilterBarProps) {
  return (
    <section className="toolbar" aria-label="Filters and actions">
      <label className="search-field">
        <Search size={18} aria-hidden="true" />
        <span className="sr-only">Search</span>
        <input
          value={query}
          onChange={(event) => onQueryChange(event.target.value)}
          placeholder="service, server, host, port"
        />
      </label>

      <select
        className="select-control"
        value={selectedService}
        onChange={(event) => onServiceChange(event.target.value)}
        aria-label="Service"
      >
        <option value="all">All services</option>
        {services.map((service) => (
          <option value={service} key={service}>
            {service}
          </option>
        ))}
      </select>

      <div className="segmented-control" aria-label="Status filter">
        {statuses.map((item) => (
          <button
            className={status === item.value ? "is-active" : ""}
            type="button"
            key={item.value}
            onClick={() => onStatusChange(item.value)}
          >
            {item.label}
          </button>
        ))}
      </div>

      <div className="toolbar-actions">
        {onAdd ? (
          <button className="icon-button" type="button" onClick={onAdd} title="Add server" aria-label="Add server">
            <Plus size={18} aria-hidden="true" />
          </button>
        ) : null}
        <button className="icon-button" type="button" onClick={onRefresh} title="Refresh" aria-label="Refresh">
          <RefreshCw size={18} className={isLoading ? "spin" : ""} aria-hidden="true" />
        </button>
      </div>
    </section>
  );
}
