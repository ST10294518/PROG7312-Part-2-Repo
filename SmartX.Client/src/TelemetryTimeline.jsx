import { useEffect, useState } from "react";

const API = "http://localhost:5206/api";

async function readJson(path, signal) {
    const response = await fetch(`${API}/${path}`, { signal });
    if (!response.ok)
        throw new Error(`History request failed (${response.status}).`);
    return response.json();
}

function NumericTimeline({ records, metric }) {
    const numeric = records.filter(record =>
        record.isNumeric && Number.isFinite(record.numericValue)
    );

    if (numeric.length === 0)
        return <p className="sx-empty">No numeric readings to plot for this metric.</p>;

    const width = 760;
    const height = 260;
    const left = 65;
    const right = 20;
    const top = 20;
    const bottom = 45;

    const times = numeric.map(record => new Date(record.timestamp).getTime());
    const values = numeric.map(record => record.numericValue);
    const first = Math.min(...times);
    const last = Math.max(...times);
    const minimum = Math.min(...values);
    const maximum = Math.max(...values);
    const padding = Math.max((maximum - minimum) * 0.1, 1);
    const low = minimum - padding;
    const high = maximum + padding;

    const x = time => last === first
        ? (left + width - right) / 2
        : left + ((time - first) / (last - first)) * (width - left - right);

    const y = value =>
        top + ((high - value) / (high - low)) * (height - top - bottom);

    const points = numeric.map((record, index) =>
        `${x(times[index])},${y(record.numericValue)}`
    ).join(" ");

    return (
        <figure className="sx-timeline">
            <svg viewBox={`0 0 ${width} ${height}`} role="img"
                aria-label={`${metric} timeline with ${numeric.length} readings`}>
                {[0, 0.5, 1].map(fraction => {
                    const value = low + (high - low) * fraction;
                    const position = y(value);
                    return (
                        <g key={fraction}>
                            <line x1={left} x2={width - right}
                                y1={position} y2={position}
                                stroke="#254252" />
                            <text x={left - 10} y={position + 4}
                                textAnchor="end" fill="#a9bfcb" fontSize="12">
                                {value.toFixed(1)}
                            </text>
                        </g>
                    );
                })}

                <polyline points={points} fill="none"
                    stroke="#55c9ed" strokeWidth="2" />

                {numeric.map((record, index) => (
                    <circle key={record.id}
                        cx={x(times[index])} cy={y(record.numericValue)}
                        r={record.isAnomaly ? 5 : 3}
                        fill={record.isAnomaly ? "#ffb56b" : "#66e6a6"}>
                        <title>
                            {new Date(record.timestamp).toLocaleString()}
                            {` · ${metric}: ${record.numericValue}`}
                            {record.isAnomaly ? " · Anomaly" : ""}
                        </title>
                    </circle>
                ))}

                <text x={left} y={height - 12}
                    fill="#a9bfcb" fontSize="12">
                    {new Date(first).toLocaleTimeString()}
                </text>
                <text x={width - right} y={height - 12}
                    textAnchor="end" fill="#a9bfcb" fontSize="12">
                    {new Date(last).toLocaleTimeString()}
                </text>
            </svg>
            <figcaption className="sx-muted">
                {metric} · Time on the horizontal axis · Value on the vertical axis.
                Orange markers indicate anomalies. Exact values are listed below.
            </figcaption>
        </figure>
    );
}

export default function TelemetryTimeline() {
    const [sensors, setSensors] = useState([]);
    const [sensorId, setSensorId] = useState("");
    const [records, setRecords] = useState([]);
    const [metric, setMetric] = useState("");
    const [loaded, setLoaded] = useState(false);
    const [error, setError] = useState("");

    useEffect(() => {
        const controller = new AbortController();
        let timer;

        async function refresh() {
            try {
                const devices = await readJson("Sensor", controller.signal);
                if (controller.signal.aborted) return;

                setSensors(devices);

                const selected = devices.find(device => device.id === sensorId)
                    ?? devices[0];

                if (!selected) {
                    setRecords([]);
                    setMetric("");
                    setLoaded(true);
                    setError("");
                    return;
                }

                if (selected.id !== sensorId) {
                    setSensorId(selected.id);
                    return;
                }

                const data = await readJson(
                    `Telemetry/sensor/${selected.id}?count=500`,
                    controller.signal
                );

                if (!controller.signal.aborted) {
                    const workingRecords = data
                        .filter(record => !/-Demo$/i.test(record.metric))
                        .slice(-100);

                    setRecords(workingRecords);
                    const metrics = [...new Set(
                        workingRecords.map(record => record.metric)
                    )];
                    setMetric(previous =>
                        metrics.includes(previous) ? previous : metrics[0] ?? ""
                    );
                    setLoaded(true);
                    setError("");
                }
            } catch (err) {
                if (!controller.signal.aborted)
                    setError(err.message || "Cannot load telemetry history.");
            } finally {
                if (!controller.signal.aborted)
                    timer = window.setTimeout(refresh, 2000);
            }
        }

        refresh();

        return () => {
            controller.abort();
            window.clearTimeout(timer);
        };
    }, [sensorId]);

    function changeDevice(event) {
        setSensorId(event.target.value);
        setRecords([]);
        setMetric("");
        setLoaded(false);
        setError("");
    }

    const metrics = [...new Set(records.map(record => record.metric))].sort();
    const visible = records.filter(record => record.metric === metric);

    function displayValue(record) {
        if (record.valueType === "bool")
            return String(record.value).toLowerCase() === "true" ? "ON" : "OFF";
        return record.value;
    }

    return (
        <section className="sx-panel" aria-labelledby="timeline-title">
            <h2 id="timeline-title">Telemetry Timeline and History</h2>
            <p className="sx-muted">
                Latest 100 readings for the selected device, ordered oldest first.
                Choose one metric to compare readings in the same units.
            </p>

            <div className="sx-filters sx-history-filters">
                <label>
                    Device
                    <select value={sensorId} onChange={changeDevice}>
                        {!sensors.length && <option value="">No devices available</option>}
                        {sensors.map(sensor => (
                            <option key={sensor.id} value={sensor.id}>
                                {sensor.deploymentLocation} · {sensor.macAddress}
                            </option>
                        ))}
                    </select>
                </label>
                <label>
                    Metric
                    <select value={metric}
                        onChange={event => setMetric(event.target.value)}>
                        {!metrics.length && <option value="">No readings available</option>}
                        {metrics.map(item => (
                            <option key={item} value={item}>{item}</option>
                        ))}
                    </select>
                </label>
            </div>

            {error && <p className="sx-error" role="alert">{error}</p>}
            {!loaded && !error && <p role="status">Loading device history…</p>}

            {loaded && visible.length === 0 && (
                <p className="sx-empty">No readings are available for this selection.</p>
            )}

            {loaded && visible.length > 0 && (
                <>
                    <NumericTimeline records={visible} metric={metric} />
                    <div className="sx-table-wrap">
                        <table className="sx-table">
                            <thead>
                                <tr>
                                    <th scope="col">Timestamp</th>
                                    <th scope="col">Metric</th>
                                    <th scope="col">Type</th>
                                    <th scope="col">Value</th>
                                    <th scope="col">Result</th>
                                </tr>
                            </thead>
                            <tbody>
                                {visible.map(record => (
                                    <tr key={record.id}>
                                        <td>{new Date(record.timestamp).toLocaleString()}</td>
                                        <td>{record.metric}</td>
                                        <td>{record.valueType}</td>
                                        <td>{displayValue(record)}</td>
                                        <td className={
                                            record.isAnomaly ? "sx-warning" : "sx-online"
                                        }>
                                            {record.isAnomaly
                                                ? record.alertMessage : "Normal"}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </>
            )}
        </section>
    );
}

