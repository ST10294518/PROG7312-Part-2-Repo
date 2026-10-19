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

    const [selectedSensor, setSelectedSensor] = useState("");
    const [metric, setMetric] = useState("Temperature");

    const [uploadSensorId, setUploadSensorId] = useState("");
    const [uploadFile, setUploadFile] = useState(null);
    const [uploadMessage, setUploadMessage] = useState("");
    const [uploadError, setUploadError] = useState("");
    const [uploading, setUploading] = useState(false);

    const [valueType, setValueType] = useState("float");
    const [value, setValue] = useState("");
    const [ingestMessage, setIngestMessage] = useState("");
    const [ingestError, setIngestError] = useState("");
    const [submitting, setSubmitting] = useState(false);

    const [macAddress, setMacAddress] = useState("");
    const [deploymentLocation, setDeploymentLocation] = useState("");
    const [sensorCategory, setSensorCategory] = useState("Environmental");
    const [registrationMessage, setRegistrationMessage] = useState("");
    const [registrationError, setRegistrationError] = useState("");
    const [registering, setRegistering] = useState(false);

    const loadDashboard = async () => {
        try {
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
                throw new Error(
                    "Unable to retrieve data from the Smart-X API."
                );
            }

            const statusData = await statusResponse.json();
            const statisticsData = await statisticsResponse.json();
            const telemetryData = await telemetryResponse.json();
            const sensorsData = await sensorsResponse.json();

            setError("");
            setStatus(statusData);
            setStatistics(statisticsData);
            setTelemetry(telemetryData);
            setSensors(sensorsData);
        } catch {
            setError(
                "Unable to connect to the Smart-X API. Make sure the API is running."
            );
        } finally {
            setLoading(false);
        }
    };

    const submitTelemetry = async (event) => {
        event.preventDefault();

        setIngestMessage("");
        setIngestError("");

        if (!selectedSensor) {
            setIngestError("Please select a sensor.");
            return;
        }

        if (!metric.trim()) {
            setIngestError("Please enter a metric.");
            return;
        }

        if (!value.trim()) {
            setIngestError("Please enter a telemetry value.");
            return;
        }

        if (valueType === "float") {
            const parsed = Number(value);

            if (!Number.isFinite(parsed)) {
                setIngestError(
                    "Float telemetry must be a valid number."
                );
                return;
            }
        }

        if (valueType === "int") {
            const parsed = Number(value);

            if (!Number.isInteger(parsed)) {
                setIngestError(
                    "Integer telemetry must be a whole number."
                );
                return;
            }
        }

        if (valueType === "bool") {
            const normalised = value.trim().toLowerCase();

            if (
                normalised !== "true" &&
                normalised !== "false"
            ) {
                setIngestError(
                    "Boolean telemetry must be true or false."
                );
                return;
            }
        }

        let parsedValue;

        if (valueType === "float") {
            parsedValue = Number(value);
        } else if (valueType === "int") {
            parsedValue = Number(value);
        } else {
            parsedValue =
                value.trim().toLowerCase() === "true";
        }

        setSubmitting(true);

        try {
            const response = await fetch(
                `${API_URL}/api/Telemetry`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        sensorId: selectedSensor,
                        metric: metric.trim(),
                        valueType: valueType,
                        value: parsedValue
                    })
                }
            );

            const data = await response.json();

            if (!response.ok) {
                throw new Error(
                    data.message ||
                    "Telemetry submission failed."
                );
            }

            setIngestMessage(data.message);
            setValue("");

            await loadDashboard();
        } catch (err) {
            setIngestError(
                err.message ||
                "Unable to submit telemetry."
            );
        } finally {
            setSubmitting(false);
        }
    };

    const handleFileUpload = async (event) => {
        event.preventDefault();

        setUploadMessage("");
        setUploadError("");

        if (!uploadSensorId) {
            setUploadError("Please select a sensor.");
            return;
        }

        if (!uploadFile) {
            setUploadError("Please select a file.");
            return;
        }

        const allowedExtensions = [
            ".json",
            ".txt",
            ".log",
            ".csv",
            ".xml",
            ".jpg",
            ".jpeg",
            ".png"
        ];

        const fileName = uploadFile.name.toLowerCase();

        const isAllowed = allowedExtensions.some(
            extension => fileName.endsWith(extension)
        );

        if (!isAllowed) {
            setUploadError(
                "Unsupported file type. Please upload a configuration file, log or image."
            );
            return;
        }

        if (uploadFile.size > 10 * 1024 * 1024) {
            setUploadError("File must be 10 MB or smaller.");
            return;
        }

        try {
            setUploading(true);

            const formData = new FormData();
            formData.append("file", uploadFile);

            const response = await fetch(
                `${API_URL}/api/File/sensor/${uploadSensorId}`,
                {
                    method: "POST",
                    body: formData
                }
            );

            const data = await response.json();

            if (!response.ok) {
                throw new Error(
                    data.message ||
                    "File upload failed."
                );
            }

            setUploadMessage(
                data.message ||
                "File uploaded successfully."
            );

            setUploadFile(null);

            event.target.reset();
        } catch (err) {
            setUploadError(
                err.message ||
                "Unable to upload file."
            );
        } finally {
            setUploading(false);
        }
    };

    const registerSensor = async (event) => {
        event.preventDefault();

        setRegistrationMessage("");
        setRegistrationError("");

        if (!macAddress.trim()) {
            setRegistrationError("MAC address is required.");
            return;
        }

        if (!deploymentLocation.trim()) {
            setRegistrationError(
                "Deployment location is required."
            );
            return;
        }

        if (!sensorCategory) {
            setRegistrationError(
                "Please select a sensor category."
            );
            return;
        }

        const macPattern =
            /^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$/;

        if (!macPattern.test(macAddress.trim())) {
            setRegistrationError(
                "Enter a valid MAC address, for example AA:BB:CC:DD:EE:04."
            );
            return;
        }

        setRegistering(true);

        try {
            const response = await fetch(
                `${API_URL}/api/Sensor`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify({
                        macAddress: macAddress.trim(),
                        deploymentLocation:
                            deploymentLocation.trim(),
                        sensorCategory
                    })
                }
            );

            const data = await response.json();

            if (!response.ok) {
                throw new Error(
                    data.message ||
                    "Sensor registration failed."
                );
            }

            setRegistrationMessage(
                "Sensor registered successfully."
            );

            setMacAddress("");
            setDeploymentLocation("");

            await loadDashboard();
        } catch (err) {
            setRegistrationError(
                err.message ||
                "Unable to register sensor."
            );
        } finally {
            setRegistering(false);
        }
    };

    useEffect(() => {
        let active = true;
        let timer;

        async function poll() {
            await loadDashboard();

            if (active)
                timer = window.setTimeout(poll, 5000);
        }

        timer = window.setTimeout(poll, 0);

        return () => {
            active = false;
            window.clearTimeout(timer);
        };
    }, []);

    const formatValue = (record) => {
        if (record.valueType === "bool") {
            return record.value === "True"
                ? "ON"
                : "OFF";
        }

        if (
            record.metric
                .toLowerCase()
                .includes("temperature")
        ) {
            return `${Number(record.value).toFixed(1)} °C`;
        }

        if (
            record.metric
                .toLowerCase()
                .includes("moisture")
        ) {
            return `${Number(record.value).toFixed(1)} %`;
        }

        if (
            record.metric
                .toLowerCase()
                .includes("power")
        ) {
            return `${Number(record.value).toFixed(0)} W`;
        }

        return record.value;
    };

    const getSensorName = (sensorId) => {
        const sensor = sensors.find(
            (s) => s.id === sensorId
        );

        return sensor
            ? sensor.deploymentLocation
            : "Unknown Sensor";
    };

    if (loading) {
        return (
            <div className="loading-screen">
                <div className="loader"></div>

                <h2>
                    Starting Smart-X Gateway
                </h2>

                <p>
                    Connecting to telemetry services...
                </p>
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

                {/* SYSTEM PILLARS */}
                <section className="system-pillars">

                    {/* PART 1 — ACTIVE */}
                    <div className="pillar-card pillar-active">

                        <div className="pillar-icon">
                            ◉
                        </div>

                        <div className="pillar-content">

                            <span className="pillar-status active">
                                ✓ ACTIVE — PART 1
                            </span>

                            <h2>
                                Sensor Data Ingestion & Telemetry
                            </h2>

                            <p>
                                Live sensor registration, telemetry
                                ingestion, monitoring and anomaly detection.
                            </p>

                        </div>

                    </div>

                    {/* PART 2 — ACTIVE */}
                    <a
                        href="#commands"
                        className="pillar-card pillar-active sx-pillar-link"
                    >
                        <div className="pillar-icon">↗</div>
                        <div className="pillar-content">
                            <span className="pillar-status active">
                                ACTIVE — PART 2
                            </span>
                            <h2>Real-Time Command Stream &amp; History</h2>
                            <p>
                                Open live processing, device commands,
                                alerts and operational history.
                            </p>
                        </div>
                    </a>

                    {/* FINAL POE — DISABLED */}
                    <div className="pillar-card pillar-disabled">

                        <div className="pillar-icon">
                            🔒
                        </div>

                        <div className="pillar-content">

                            <span className="pillar-status disabled">
                                🔒 COMING IN FINAL POE
                            </span>

                            <h2>
                                Network Topology & Mesh Routing
                            </h2>

                            <p>
                                Mesh topology, routing and network
                                optimisation will be implemented
                                in the final PoE phase.
                            </p>

                        </div>

                    </div>

                </section>

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

                        <span className="stat-icon">
                            ◉
                        </span>

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

                        <span className="stat-icon">
                            ⚠
                        </span>

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

                        <span className="stat-icon">
                            ⌁
                        </span>

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

                        <span className="stat-icon">
                            ≈
                        </span>

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

                {/* TELEMETRY INGESTION */}
                <section className="panel ingestion-panel">

                    <div className="panel-header">

                        <div>

                            <span className="panel-kicker">
                                DATA INGESTION
                            </span>

                            <h2>
                                Send Telemetry
                            </h2>

                        </div>

                        <span className="ingestion-badge">
                            API POST
                        </span>

                    </div>

                    <form
                        className="ingestion-form"
                        onSubmit={submitTelemetry}
                    >

                        <div className="form-field">

                            <label>
                                Sensor
                            </label>

                            <select
                                value={selectedSensor}
                                onChange={(event) =>
                                    setSelectedSensor(
                                        event.target.value
                                    )
                                }
                            >

                                <option value="">
                                    Select a sensor
                                </option>

                                {sensors.map((sensor) => (

                                    <option
                                        key={sensor.id}
                                        value={sensor.id}
                                    >
                                        {sensor.deploymentLocation} —{" "}
                                        {sensor.sensorCategory}
                                    </option>

                                ))}

                            </select>

                        </div>

                        <div className="form-field">

                            <label>
                                Metric
                            </label>

                            <input
                                type="text"
                                value={metric}
                                onChange={(event) =>
                                    setMetric(
                                        event.target.value
                                    )
                                }
                                placeholder="Example: Temperature"
                            />

                        </div>

                        <div className="form-field">

                            <label>
                                Value Type
                            </label>

                            <select
                                value={valueType}
                                onChange={(event) =>
                                    setValueType(
                                        event.target.value
                                    )
                                }
                            >

                                <option value="float">
                                    Float
                                </option>

                                <option value="int">
                                    Integer
                                </option>

                                <option value="bool">
                                    Boolean
                                </option>

                            </select>

                        </div>

                        <div className="form-field">

                            <label>
                                Value
                            </label>

                            <input
                                type="text"
                                value={value}
                                onChange={(event) =>
                                    setValue(
                                        event.target.value
                                    )
                                }
                                placeholder={
                                    valueType === "bool"
                                        ? "true or false"
                                        : "Enter telemetry value"
                                }
                            />

                        </div>

                        <button
                            type="submit"
                            className="send-button"
                            disabled={submitting}
                        >
                            {submitting
                                ? "Sending..."
                                : "➤ Send Telemetry"}
                        </button>

                    </form>

                    {ingestMessage && (
                        <div className="ingest-success">
                            ✓ {ingestMessage}
                        </div>
                    )}

                    {ingestError && (
                        <div className="ingest-error">
                            ⚠ {ingestError}
                        </div>
                    )}

                </section>

                {/* SENSOR REGISTRATION */}
                <section className="panel registration-panel">

                    <div className="panel-header">

                        <div>

                            <span className="panel-kicker">
                                SENSOR MANAGEMENT
                            </span>

                            <h2>
                                Register Sensor
                            </h2>

                        </div>

                        <span className="ingestion-badge">
                            API POST
                        </span>

                    </div>

                    <form
                        className="registration-form"
                        onSubmit={registerSensor}
                    >

                        <div className="form-field">

                            <label>
                                MAC Address
                            </label>

                            <input
                                type="text"
                                value={macAddress}
                                onChange={(event) =>
                                    setMacAddress(
                                        event.target.value
                                    )
                                }
                                placeholder="AA:BB:CC:DD:EE:04"
                            />

                        </div>

                        <div className="form-field">

                            <label>
                                Deployment Location
                            </label>

                            <input
                                type="text"
                                value={deploymentLocation}
                                onChange={(event) =>
                                    setDeploymentLocation(
                                        event.target.value
                                    )
                                }
                                placeholder="Example: Server Room"
                            />

                        </div>

                        <div className="form-field">

                            <label>
                                Sensor Category
                            </label>

                            <select
                                value={sensorCategory}
                                onChange={(event) =>
                                    setSensorCategory(
                                        event.target.value
                                    )
                                }
                            >

                                <option value="Environmental">
                                    Environmental
                                </option>

                                <option value="Power Consumption">
                                    Power Consumption
                                </option>

                                <option value="Actuator">
                                    Actuator
                                </option>

                            </select>

                        </div>

                        <button
                            type="submit"
                            className="send-button"
                            disabled={registering}
                        >
                            {registering
                                ? "Registering..."
                                : "＋ Register Sensor"}
                        </button>

                    </form>

                    {registrationMessage && (
                        <div className="ingest-success">
                            ✓ {registrationMessage}
                        </div>
                    )}

                    {registrationError && (
                        <div className="ingest-error">
                            ⚠ {registrationError}
                        </div>
                    )}

                </section>

                {/* MEDIA / LOG UPLOAD */}
                <section className="panel upload-panel">

                    <div className="panel-header">

                        <div>

                            <span className="panel-kicker">
                                MEDIA / LOG UPLOAD
                            </span>

                            <h2>
                                Attach Sensor Files
                            </h2>

                        </div>

                        <span className="ingestion-badge">
                            MULTIPART POST
                        </span>

                    </div>

                    <form
                        className="registration-form"
                        onSubmit={handleFileUpload}
                    >

                        <div className="form-field">

                            <label>
                                Sensor
                            </label>

                            <select
                                value={uploadSensorId}
                                onChange={(event) =>
                                    setUploadSensorId(
                                        event.target.value
                                    )
                                }
                            >

                                <option value="">
                                    Select a sensor
                                </option>

                                {sensors.map((sensor) => (

                                    <option
                                        key={sensor.id}
                                        value={sensor.id}
                                    >
                                        {sensor.deploymentLocation} —{" "}
                                        {sensor.sensorCategory}
                                    </option>

                                ))}

                            </select>

                        </div>

                        <div className="form-field">

                            <label>
                                Configuration, Log or Image
                            </label>

                            <input
                                type="file"
                                accept=".json,.txt,.log,.csv,.xml,.jpg,.jpeg,.png"
                                onChange={(event) =>
                                    setUploadFile(
                                        event.target.files?.[0] || null
                                    )
                                }
                            />

                        </div>

                        <button
                            type="submit"
                            className="send-button"
                            disabled={uploading}
                        >
                            {uploading
                                ? "Uploading..."
                                : "↑ Upload File"}
                        </button>

                    </form>

                    {uploadMessage && (
                        <div className="ingest-success">
                            ✓ {uploadMessage}
                        </div>
                    )}

                    {uploadError && (
                        <div className="ingest-error">
                            ⚠ {uploadError}
                        </div>
                    )}

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
                                                    {getSensorName(
                                                        record.sensorId
                                                    )}
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
                                .filter(
                                    (record) => record.isAnomaly
                                )
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
                                                {getSensorName(
                                                    record.sensorId
                                                )}
                                            </small>

                                        </div>

                                    </div>

                                ))}

                            {telemetry.filter(
                                (record) => record.isAnomaly
                            ).length === 0 && (

                                    <div className="empty-alerts">

                                        <span>
                                            ✓
                                        </span>

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

                                    {/* ACTUAL SENSOR STATUS */}
                                    <span
                                        className={
                                            sensor.status === "Online"
                                                ? "online"
                                                : "offline"
                                        }
                                    >
                                        {sensor.status === "Online"
                                            ? "● Online"
                                            : "⚠ Disconnected"}
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


