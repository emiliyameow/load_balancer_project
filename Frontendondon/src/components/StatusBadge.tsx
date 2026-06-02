import { CircleCheck, CircleHelp, CircleX } from "lucide-react";
import type { HealthStatus } from "../types/backend";

interface StatusBadgeProps {
  status: HealthStatus;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const icon =
    status === "alive" ? (
      <CircleCheck size={16} aria-hidden="true" />
    ) : status === "down" ? (
      <CircleX size={16} aria-hidden="true" />
    ) : (
      <CircleHelp size={16} aria-hidden="true" />
    );

  return (
    <span className={`status-badge status-badge--${status}`}>
      {icon}
      {status}
    </span>
  );
}
