import { useCallback, useEffect, useRef, useState } from "react";

const API = "http://localhost:5206/api";

async function request(path, options = {}) {
    const response = await fetch(`${API}/${path}`, options);
    const data = await response.json();

    if (!response.ok)
        throw new Error(data.message || `Request failed (${response.status}).`);

    return data;
}

const stateLabel = value => value ? "ON" : "OFF";
const timeLabel = value => new Date(value).toLocaleString();

export default function ActuatorControls() {
    const [devices, setDevices] = useState([]);
    const [history, setHistory] = useState([]);
    const [undoCount, setUndoCount] = useState(0);
    const [loaded, setLoaded] = useState(false);
    const [loadError, setLoadError] = useState("");
    const [actionError, setActionError] = useState("");
    const [message, setMessage] = useState("");
    const [busy, setBusy] = useState(false);

    const busyRef = useRef(false);
    const requestVersion = useRef(0);

    const refresh = useCallback(async signal => {
        const version = ++requestVersion.current;

        try {
            const [sensors, events, status] = await Promise.all([
                request("Sensor", { signal }),
                request("Command/history?count=50", { signal }),
                request("Command/status", { signal })
            ]);

            const actuators = sensors.filter(sensor =>
                sensor.sensorCategory.toLowerCase() === "actuator"
            );

            const states = await Promise.all(actuators.map(async sensor => {
                const current = await request(
                    `Command/state/${sensor.id}`, { signal }
                );
                return { ...sensor, state: current.state };
            }));

            if (!signal?.aborted && version === requestVersion.current) {
                setDevices(states);
                setHistory(events);
                setUndoCount(status.undoCount);
                setLoaded(true);
                setLoadError("");
            }
        } catch (err) {
            if (!signal?.aborted && version === requestVersion.current)
                setLoadError(err.message || "Cannot load actuator information.");
        }
    }, []);

    useEffect(() => {
        const controller = new AbortController();
        let timer;

        async function poll() {
            await refresh(controller.signal);
            if (!controller.signal.aborted)
                timer = window.setTimeout(poll, 2000);
        }

        poll();

        return () => {
            controller.abort();
            window.clearTimeout(timer);

        };
    }, [refresh]);

    async function perform(path, payload) {
        if (busyRef.current) return;

        busyRef.current = true;
        setBusy(true);
        setActionError("");
        setMessage("");

        try {
            const result = await request(path, {
                method: "POST",
                ...(payload ? {
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(payload)
                } : {})
            });

            setMessage(
                `${result.message} Current state: ${stateLabel(result.currentState)}.`
            );
        } catch (err) {
            setActionError(err.message || "Command could not be completed.");
        } finally {
            await refresh();
            busyRef.current = false;
            setBusy(false);
        }
    }

    const names = Object.fromEntries(devices.map(device => [
        device.id, device.deploymentLocation
    ]));

    return (
        <section className="sx-panel" aria-labelledby="actuator-title">
            <div className="sx-section-heading">
                <div>
                    <h2 id="actuator-title">Actuator Commands</h2>
                    <p className="sx-muted">
                        Set a device state or restore the last successful command.
                    </p>
                </div>
                <button type="button"
                    disabled={busy || !loaded || !!loadError || undoCount === 0}
                    onClick={() => perform("Command/undo")}>
                    Undo latest command ({undoCount})
                </button>
            </div>

            {loadError && <p className="sx-error" role="alert">{loadError}</p>}
            {actionError && <p className="sx-error" role="alert">{actionError}</p>}
            <p className="sx-feedback" role="status">{message}</p>

            {!loaded && !loadError && <p role="status">Loading actuators…</p>}

            {loaded && devices.length === 0 && (
                <p className="sx-empty">No actuators are registered.</p>
            )}

            <div className="sx-device-grid">
                {devices.map(device => (
                    <article className="sx-device" key={device.id}>
                        <h3>{device.deploymentLocation}</h3>
                        <p className="sx-muted">{device.macAddress}</p>
                        <p className={
                            device.status === "Online" ? "sx-online" : "sx-warning"
                        }>
                            {device.status} · State: {stateLabel(device.state)}
                        </p>
                        <div className="sx-button-row">
                            <button type="button"
                                disabled={busy || !!loadError ||
                                    device.status !== "Online" || device.state}
                                onClick={() => perform("Command", {
                                    sensorId: device.id,
                                    state: true
                                })}>
                                Turn ON
                            </button>
                            <button type="button"
                                disabled={busy || !!loadError ||
                                    device.status !== "Online" || !device.state}
                                onClick={() => perform("Command", {
                                    sensorId: device.id,
                                    state: false
                                })}>
                                Turn OFF
                            </button>
                        </div>
                    </article>
                ))}
            </div>

            <div className="sx-inspection">
                <h3>Command History</h3>
                <p className="sx-muted">
                    Latest 50 command and undo events, newest first.
                    Undo applies across all devices.
                </p>

                {loaded && history.length === 0 ? (
                    <p className="sx-empty">No commands have been issued yet.</p>
                ) : (
                    <div className="sx-table-wrap">
                        <table className="sx-table">
                            <thead>
                                <tr>
                                    <th scope="col">Time</th>
                                    <th scope="col">Device</th>
                                    <th scope="col">Action</th>
                                    <th scope="col">Previous</th>
                                    <th scope="col">Result</th>
                                </tr>
                            </thead>
                            <tbody>
                                {history.map(entry => (
                                    <tr key={
                                        `${entry.commandId}-${entry.action}-${entry.timestamp}`
                                    }>
                                        <td>{timeLabel(entry.timestamp)}</td>
                                        <td>{names[entry.sensorId] ?? entry.sensorId}</td>
                                        <td>{entry.action === "Undo"
                                            ? "Undo" : "Manual command"}</td>
                                        <td>{stateLabel(entry.previousState)}</td>
                                        <td>{stateLabel(entry.newState)}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </section>
    );
}

