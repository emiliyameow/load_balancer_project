interface Metric {
  label: string;
  value: string | number;
}

interface MetricStripProps {
  metrics: Metric[];
}

export function MetricStrip({ metrics }: MetricStripProps) {
  return (
    <section className="metric-strip" aria-label="Runtime summary">
      {metrics.map((metric) => (
        <div className="metric" key={metric.label}>
          <span>{metric.label}</span>
          <strong>{metric.value}</strong>
        </div>
      ))}
    </section>
  );
}
