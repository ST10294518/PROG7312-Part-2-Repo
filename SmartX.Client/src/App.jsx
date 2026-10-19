import { lazy, Suspense, useEffect, useState } from "react";
import TelemetryDashboard from "./TelemetryDashboard";
import "./PartTwo.css";

const CommandDashboard = lazy(() => import("./CommandDashboard"));

const currentView = () =>
    window.location.hash === "#commands" ? "commands" : "telemetry";

export default function App() {
    const [view, setView] = useState(currentView);
    const [commandsOpened, setCommandsOpened] = useState(
        () => currentView() === "commands"
    );

    useEffect(() => {
        const navigate = () => {
            const next = currentView();
            setView(next);
            if (next === "commands") setCommandsOpened(true);
        };

        window.addEventListener("hashchange", navigate);
        return () => window.removeEventListener("hashchange", navigate);
    }, []);

    return (
        <div className="sx-shell">
            <nav className="sx-nav" aria-label="Smart-X modules">
                <strong>SMART-X <span>PART 2</span></strong>
                <a href="#telemetry"
                    aria-current={view === "telemetry" ? "page" : undefined}>
                    Sensor Data Ingestion and Telemetry
                </a>
                <a href="#commands"
                    aria-current={view === "commands" ? "page" : undefined}>
                    Real-Time Command Stream and History
                </a>
                <span className="sx-future">
                    Network Topology and Mesh Routing · Part 3
                </span>
            </nav>

            <div hidden={view !== "telemetry"}>
                <TelemetryDashboard />
            </div>

            {commandsOpened && (
                <div hidden={view !== "commands"}>
                    <Suspense fallback={
                        <p className="sx-loading" role="status">
                            Loading command dashboard…
                        </p>
                    }>
                        <CommandDashboard />
                    </Suspense>
                </div>
            )}
        </div>
    );
}
