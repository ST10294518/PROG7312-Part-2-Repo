import { useEffect, useMemo, useState } from "react";

const API = "http://localhost:5206/api";

async function readJson(path, signal) {
    const response = await fetch(`${API}/${path}`, { signal });
    if (!response.ok)
        throw new Error(`Device data request failed (${response.status}).`);
    return response.json();
}

export default function DeviceOperations() {
    const [sensors, setSensors] = useState([]);
    const [alerts, setAlerts] = useState([]);
    const [query, setQuery] = useState("");
    const [category, setCategory] = useState("");
    const [condition, setCondition] = useState("");
    const [selectedId, setSelectedId] = useState("");
    const [loaded, setLoaded] = useState(false);
    const [error, setError] = useState("");
    const [actionError, setActionError] = useState("");
    const [message, setMessage] = useState("");
    const [busy, setBusy] = useState(false);

    useEffect(() => {
        const controller = new AbortController();
        let timer;

        async function refresh() {
            try {
                const [devices, active] = await Promise.all([
                    readJson("Sensor", controller.signal),
                    readJson("Alert", controller.signal)
                ]);

                if (!controller.signal.aborted) {
                    setSensors(devices);
                    setAlerts(active);
                    setLoaded(true);
                    setError("");
                }
            } catch (err) {
                if (!controller.signal.aborted)
                    setError(err.message || "Cannot load device information.");
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
    }, []);

    const alertsByDevice = useMemo(() => {
        const grouped = new Map();
        for (const alert of alerts) {
            const items = grouped.get(alert.sensorId) ?? [];
            items.push(alert);
            grouped.set(alert.sensorId, items);
        }
        return grouped;
    }, [alerts]);

    const categories = [...new Set(sensors.map(s => s.sensorCategory))].sort();
    const search = query.trim().toLowerCase();

    const visible = sensors.filter(sensor => {
        const text = [
            sensor.id,
            sensor.macAddress,
            sensor.deploymentLocation,
            sensor.sensorCategory
        ].join(" ").toLowerCase();

        const hasAlert = (alertsByDevice.get(sensor.id)?.length ?? 0) > 0;

        return (!search || text.includes(search)) &&
            (!category || sensor.sensorCategory === category) &&
            (!condition ||
                (condition === "alert" && hasAlert) ||
                (condition === "clear" && !hasAlert) ||
                (condition === "offline" && sensor.status === "Disconnected"));
    });

    const selected = sensors.find(sensor => sensor.id === selectedId);
    const selectedAlerts = alertsByDevice.get(selectedId) ?? [];

    async function inspect(sensor) {
        if (busy) return;
        setBusy(true);
        setActionError("");
        setMessage("");

        try {
            if (query.trim()) {
                const response = await fetch(`${API}/Recommendation/searches`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        sensorId: sensor.id,
                        query: query.trim()
                    })
                });

                const data = await response.json();
                if (!response.ok)
                    throw new Error(data.message || "Cannot record this search.");

                setMessage("Device selected. Search recorded.");
            } else {
                setMessage("Device selected.");
            }

            setSelectedId(sensor.id);
        } catch (err) {
            setActionError(err.message || "Cannot select this device.");
        } finally {
            setBusy(false);
        }
    }

    return (
        <section className="sx-panel" aria-labelledby="device-title">
            <h2 id="device-title">Devices and Active Alerts</h2>
            <p className="sx-muted">
                Find a device by location, MAC address, category or ID.
            </p>

            <div className="sx-filters">
                <label>
                    Search devices
                    <input value={query} maxLength={100}
                        placeholder="For example: Greenhouse"
                        onChange={event => setQuery(event.target.value)} />
                </label>
                <label>
                    Category
                    <select value={category}
                        onChange={event => setCategory(event.target.value)}>
                        <option value="">All categories</option>
                        {categories.map(item => (
                            <option key={item} value={item}>{item}</option>
                        ))}
                    </select>
                </label>
                <label>
                    Device condition
                    <select value={condition}
                        onChange={event => setCondition(event.target.value)}>
                        <option value="">All devices</option>
                        <option value="alert">Has active alerts</option>
                        <option value="clear">No active alerts</option>
                        <option value="offline">Disconnected</option>
                    </select>
                </label>
            </div>

            {error && <p className="sx-error" role="alert">{error}</p>}
            {actionError && <p className="sx-error" role="alert">{actionError}</p>}
            <p className="sx-feedback" role="status">{message}</p>

            {!loaded && !error && <p role="status">Loading devices…</p>}

            {loaded && (
                <>
                    <p className="sx-muted">
                        {visible.length} of {sensors.length} devices shown
                        {" · "}{alerts.length} active alerts
                    </p>
                    {visible.length === 0 ? (
                        <p className="sx-empty">No devices match these filters.</p>
                    ) : (
                        <div className="sx-device-grid">
                            {visible.map(sensor => {
                                const count = alertsByDevice.get(sensor.id)?.length ?? 0;
                                return (
                                    <article className="sx-device" key={sensor.id}>
                                        <h3>{sensor.deploymentLocation}</h3>
                                        <p>{sensor.sensorCategory}</p>
                                        <p className="sx-muted">{sensor.macAddress}</p>
                                        <p className={
                                            sensor.status === "Online"
                                                ? "sx-online" : "sx-warning"
                                        }>
                                            {sensor.status} · {count} active alerts
                                        </p>
                                        <button type="button" disabled={busy}
                                            aria-pressed={selectedId === sensor.id}
                                            onClick={() => inspect(sensor)}>
                                            {selectedId === sensor.id
                                                ? "Selected device" : "Inspect device"}
                                        </button>
                                    </article>
                                );
                            })}
                        </div>
                    )}
                </>
            )}

            {selected && (
                <div className="sx-inspection">
                    <h3>Selected: {selected.deploymentLocation}</h3>
                    <p className="sx-muted">{selected.id}</p>
                    {selectedAlerts.length === 0 ? (
                        <p className="sx-empty">This device has no active alerts.</p>
                    ) : (
                        <ul className="sx-alert-list">
                            {selectedAlerts.map(alert => (
                                <li key={alert.faultType}>
                                    <strong>{alert.message}</strong>
                                    <span className="sx-muted">
                                        First seen: {new Date(alert.firstSeen).toLocaleString()}
                                        {" · "}Observations: {alert.occurrences}
                                    </span>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>
            )}
        </section>
    );
}
