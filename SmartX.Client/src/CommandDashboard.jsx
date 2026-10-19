import DeviceOperations from "./DeviceOperations";
import { useEffect, useState } from "react";

const API = "http://localhost:5206/api";

export default function CommandDashboard() {
    const [status, setStatus] = useState(null);
    const [error, setError] = useState("");
    const [updated, setUpdated] = useState(null);

    useEffect(() => {
        const controller = new AbortController();
        let timer;

        async function refresh() {
            try {
                const response = await fetch(`${API}/Telemetry/queue`, {
                    signal: controller.signal
                });

                if (!response.ok)
                    throw new Error(`Queue status request failed (${response.status}).`);

                const data = await response.json();

                if (!controller.signal.aborted) {
                    setStatus(data);
                    setUpdated(new Date());
                    setError("");
                }
            } catch (err) {
                if (!controller.signal.aborted)
                    setError(err.message || "Cannot connect to the API.");
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

    const cards = [
        ["Standard waiting", status?.queue.standardPending],
        ["Urgent waiting", status?.queue.urgentPending],
        ["Processed", status?.processed],
        ["Failed", status?.failed],
        ["Rejected", status?.queue.rejected]
    ];

    return (
        <main className="sx-command-page">
            <header className="sx-page-heading">
                <p className="sx-eyebrow">LIVE DEVICE OPERATIONS</p>
                <h1>Real-Time Command Stream and History</h1>
                <p>Monitor incoming telemetry and gateway processing.</p>
            </header>

            {error && <p className="sx-error" role="alert">{error}</p>}

            {!status && !error && (
                <p role="status">Connecting to the gateway…</p>
            )}

            {status && (
                <>
                    <section className="sx-stats" aria-label="Processing summary">
                        {cards.map(([label, value]) => (
                            <article className="sx-card" key={label}>
                                <span>{label}</span>
                                <strong>{Number(value ?? 0).toLocaleString()}</strong>
                            </article>
                        ))}
                    </section>
                    <p className="sx-muted">
                        Last updated: {updated?.toLocaleTimeString()}
                        {error ? " · Showing previously received data" : ""}
                    </p>
                </>
            )}
            <DeviceOperations />
        </main>
    );
}

