import { useEffect, useState } from "react";
import "./App.css";

const API_URL = "http://localhost:5206";

function App() {
    const [status, setStatus] = useState(null);
    const [statistics, setStatistics] = useState(null);
    const [telemetry, setTelemetry] = useState([]);
    const [sensors, setSensors] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const loadDashboard = async () => {
        try {
            setError("");

            const [
                statusResponse,
                statisticsResponse,
                telemetryResponse,
                sensorsResponse
            ] = await Promise.all([
                fetch(`${API_URL}/api/System/status`),
                fetch(`${API_URL}/api/Telemetry/statistics`),
                fetch(`${API_URL}/api/Telemetry?count=20`),
                fetch(`${API_URL}/api/Sensor`)
            ]);

            if (
                !statusResponse.ok ||
                !statisticsResponse.ok ||
                !telemetryResponse.ok ||
                !sensorsResponse.ok
            ) {
                throw new Error("Unable to retrieve data from the Smart-X API.");
            }

            const statusData = await statusResponse.json();
            const statisticsData = await statisticsResponse.json();
            const telemetryData = await telemetryResponse.json();
            const sensorsData = await sensorsResponse.json();

            setStatus(statusData);
            setStatistics(statisticsData);
            setTelemetry(telemetryData);
            setSensors(sensorsData);
        } catch (err) {
            setError(
                "Unable to connect to the Smart-X API. Make sure the API is running."
            );
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadDashboard();

        const interval = setInterval(loadDashboard, 5000);

        return () => clearInterval(interval);
    }, []);

    const formatValue = (record) => {
        if (record.valueType === "bool") {
            return record.value === "True" ? "ON" : "OFF";
        }

        if (record.metric.toLowerCase().includes("temperature")) {
            return `${Number(record.value).toFixed(1)} °C`;
        }

        if (record.metric.toLowerCase().includes("moisture")) {
            return `${Number(record.value).toFixed(1)} %`;
        }

        if (record.metric.toLowerCase().includes("power")) {
            return `${Number(record.value).toFixed(0)} W`;
        }

        return record.value;
    };

    const getSensorName = (sensorId) => {
        const sensor = sensors.find((s) => s.id === sensorId);

        return sensor
            ? sensor.deploymentLocation
            : "Unknown Sensor";
    };

    if (loading) {
        return (
            <div className="loading-screen">
                <div className="loader"></div>
                <h2>Starting Smart-X Gateway</h2>
                <p>Connecting to telemetry services...</p>
            </div>
        );
    }

    return (
        <div className="app">

            {/* HEADER */}
            <header className="topbar">
                <div>
                    <div className="brand">
                        SMART<span>-X</span>
                    </div>

                    <div className="subtitle">
                        IoT Mesh Ecosystem
                    </div>
                </div>

                <div className="gateway-status">
                    <span className="status-dot"></span>

                    <div>
                        <strong>
                            {status?.status || "Offline"}
                        </strong>

                        <small>
                            Gateway {status?.version || ""}
                        </small>
                    </div>
                </div>
            </header>

            {/* MAIN CONTENT */}
            <main className="dashboard">

                {error && (
                    <div className="error-banner">
                        ⚠ {error}
                    </div>
                )}

                {/* PAGE TITLE */}
                <section className="page-heading">
                    <div>
                        <p className="eyebrow">
                            PART 1 / ACTIVE MODULE
                        </p>

                        <h1>
                            Sensor Data Ingestion
                        </h1>

                        <p>
                            Real-time telemetry monitoring and anomaly detection
                        </p>
                    </div>

                    <button
                        className="refresh-button"
                        onClick={loadDashboard}
                    >
                        ↻ Refresh
                    </button>
                </section>

                {/* STATISTICS */}
                <section className="stats-grid">

                    <div className="stat-card">
                        <span className="stat-icon">◉</span>

                        <div>
                            <span className="stat-label">
                                Total Readings
                            </span>

                            <strong>
                                {statistics?.totalReadings ?? 0}
                            </strong>
                        </div>
                    </div>

                    <div className="stat-card alert-stat">
                        <span className="stat-icon">⚠</span>

                        <div>
                            <span className="stat-label">
                                Anomalies
                            </span>

                            <strong>
                                {statistics?.totalAlerts ?? 0}
                            </strong>
                        </div>
                    </div>

                    <div className="stat-card">
                        <span className="stat-icon">⌁</span>

                        <div>
                            <span className="stat-label">
                                Environmental
                            </span>

                            <strong>
                                {statistics?.environmentalReadings ?? 0}
                            </strong>
                        </div>
                    </div>

                    <div className="stat-card">
                        <span className="stat-icon">≈</span>

                        <div>
                            <span className="stat-label">
                                Average Numeric
                            </span>

                            <strong>
                                {Number(
                                    statistics?.averageNumericValue ?? 0
                                ).toFixed(1)}
                            </strong>
                        </div>
                    </div>

                </section>

                {/* TWO COLUMN SECTION */}
                <section className="content-grid">

                    {/* TELEMETRY */}
                    <div className="panel telemetry-panel">

                        <div className="panel-header">
                            <div>
                                <span className="panel-kicker">
                                    LIVE STREAM
                                </span>

                                <h2>
                                    Latest Telemetry
                                </h2>
                            </div>

                            <span className="live-badge">
                                ● LIVE
                            </span>
                        </div>

                        <div className="table-wrapper">

                            <table>

                                <thead>
                                    <tr>
                                        <th>Sensor</th>
                                        <th>Metric</th>
                                        <th>Value</th>
                                        <th>Status</th>
                                    </tr>
                                </thead>

                                <tbody>

                                    {telemetry.map((record) => (
                                        <tr key={record.id}>

                                            <td>
                                                <span className="sensor-name">
                                                    {getSensorName(record.sensorId)}
                                                </span>
                                            </td>

                                            <td>
                                                {record.metric}
                                            </td>

                                            <td className="value-cell">
                                                {formatValue(record)}
                                            </td>

                                            <td>

                                                {record.isAnomaly ? (
                                                    <span className="status anomaly">
                                                        ⚠ Alert
                                                    </span>
                                                ) : (
                                                    <span className="status normal">
                                                        ● Normal
                                                    </span>
                                                )}

                                            </td>

                                        </tr>
                                    ))}

                                </tbody>

                            </table>

                        </div>

                    </div>

                    {/* ALERTS */}
                    <div className="panel alerts-panel">

                        <div className="panel-header">
                            <div>
                                <span className="panel-kicker">
                                    SECURITY / MONITORING
                                </span>

                                <h2>
                                    Anomaly Alerts
                                </h2>
                            </div>

                            <span className="alert-count">
                                {telemetry.filter(
                                    (record) => record.isAnomaly
                                ).length}
                            </span>
                        </div>

                        <div className="alerts-list">

                            {telemetry
                                .filter((record) => record.isAnomaly)
                                .slice(0, 8)
                                .map((record) => (

                                    <div
                                        className="alert-item"
                                        key={record.id}
                                    >

                                        <div className="alert-symbol">
                                            ⚠
                                        </div>

                                        <div>
                                            <strong>
                                                {record.metric}
                                            </strong>

                                            <p>
                                                {record.alertMessage}
                                            </p>

                                            <small>
                                                {getSensorName(record.sensorId)}
                                            </small>
                                        </div>

                                    </div>

                                ))}

                            {telemetry.filter(
                                (record) => record.isAnomaly
                            ).length === 0 && (

                                    <div className="empty-alerts">
                                        <span>✓</span>

                                        <strong>
                                            No active anomalies
                                        </strong>

                                        <p>
                                            All monitored telemetry is within
                                            expected ranges.
                                        </p>
                                    </div>

                                )}

                        </div>

                    </div>

                </section>

                {/* SENSOR OVERVIEW */}
                <section className="panel">

                    <div className="panel-header">

                        <div>
                            <span className="panel-kicker">
                                REGISTERED DEVICES
                            </span>

                            <h2>
                                Sensor Overview
                            </h2>
                        </div>

                        <span className="sensor-count">
                            {sensors.length} Sensors
                        </span>

                    </div>

                    <div className="sensor-grid">

                        {sensors.map((sensor) => (

                            <div
                                className="sensor-card"
                                key={sensor.id}
                            >

                                <div className="sensor-card-top">

                                    <div className="device-icon">
                                        {sensor.sensorCategory ===
                                            "Environmental"
                                            ? "◌"
                                            : sensor.sensorCategory ===
                                                "Power Consumption"
                                                ? "ϟ"
                                                : "⚙"}
                                    </div>

                                    <span className="online">
                                        ● Online
                                    </span>

                                </div>

                                <h3>
                                    {sensor.deploymentLocation}
                                </h3>

                                <p>
                                    {sensor.sensorCategory}
                                </p>

                                <div className="mac">
                                    {sensor.macAddress}
                                </div>

                                <div className="sensor-footer">

                                    <span>
                                        Readings
                                    </span>

                                    <strong>
                                        {sensor.totalReadings}
                                    </strong>

                                    <span>
                                        Alerts
                                    </span>

                                    <strong>
                                        {sensor.totalAlerts}
                                    </strong>

                                </div>

                            </div>

                        ))}

                    </div>

                </section>

                {/* FOOTER */}
                <footer>
                    <span>
                        SMART-X IoT Mesh Ecosystem
                    </span>

                    <span>
                        Sensor Data Ingestion & Telemetry
                    </span>
                </footer>

            </main>

        </div>
    );
}

export default App;