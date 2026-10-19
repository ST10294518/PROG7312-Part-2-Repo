# Smart-X IoT Mesh Ecosystem

## Programming 3B - Part 2

Smart-X simulates an IoT gateway for environmental sensors, power meters and actuators. Part 2 extends the Part 1 ingestion dashboard with queued telemetry processing, manual commands and undo, device filtering, active fault tracking, ordered timelines and learned action suggestions.

Backend: ASP.NET Core Web API targeting .NET 10.
Frontend: React with Vite.

## Implemented modules

1. Sensor Data Ingestion and Telemetry: registration, typed readings, anomaly detection, encrypted attachments and recursive deployment validation.
2. Real-Time Command Stream and History: live processing counters, device search and filters, alerts, actuator commands, undo, history, timelines and Suggested Actions.
3. Network Topology and Mesh Routing: reserved for the final PoE.

The frontend uses hash navigation. Both views remain mounted after opening, preserving form values and selections during navigation. Refreshing the browser resets frontend form state.

## Requirements

- .NET 10 SDK.
- Node.js and npm.
- Git.
- PowerShell for the commands below.

Development was verified using .NET SDK 10.0.401, Node.js 26.8.1 and npm 11.19.0.

## Obtain the source

```powershell
git clone --branch Develop-Branch "https://github.com/ST10294518/PROG7312-Part-2-Repo.git" "SmartX-Part2"
Set-Location ".\SmartX-Part2"
```

Run commands from the repository root unless stated otherwise.

## Restore and build

```powershell
dotnet restore .\SmartX.API\SmartX.API.csproj
dotnet build .\SmartX.API\SmartX.API.csproj --no-restore

Push-Location .\SmartX.Client
npm ci
npm run build
Pop-Location
```

Check each command succeeds before proceeding.

## Configure encrypted uploads on a new machine

The encryption key is stored in .NET User Secrets. It is not included in Git.

Generate a key once for a new local installation. Keep an existing key if it is needed for previously encrypted files.

```powershell
$bytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()

try {
    $rng.GetBytes($bytes)
}
finally {
    $rng.Dispose()
}

$uploadKey = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "SmartX:EncryptionKey" "$uploadKey" `
    --project .\SmartX.API\SmartX.API.csproj | Out-Null

Remove-Variable uploadKey, bytes
```

Uploads accept JSON, TXT, LOG, CSV, XML, JPG, JPEG and PNG files, with a configured 10 MB file limit. Files are encrypted with AES and stored in sensor-specific folders under `SmartX.API/Uploads`. Runtime uploads are ignored by Git.

## Run the application

### Terminal 1 - API

From the repository root:

```powershell
Set-Location .\SmartX.API
dotnet run --launch-profile http
```

API: http://localhost:5206  
Swagger: http://localhost:5206/swagger

### Terminal 2 - frontend

From the repository root in a separate terminal:

```powershell
Set-Location .\SmartX.Client
npm run dev -- --port 5173 --strictPort
```

Frontend: http://localhost:5173  
Command dashboard: http://localhost:5173/#commands

Leave both terminals running. Use a third terminal for API checks.

The HTTP launch profile is for local demonstration. HTTPS redirection may log that no HTTPS port is configured. HTTPS deployment configuration is outside this local setup.

## Simulation

Three devices are seeded on API startup:

| Category | Location | MAC address |
|---|---|---|
| Environmental | Greenhouse Zone A | AA:BB:CC:DD:EE:01 |
| Power Consumption | Utility Room | AA:BB:CC:DD:EE:02 |
| Actuator | Control Room | AA:BB:CC:DD:EE:03 |

The simulator sends HTTP requests to the telemetry endpoint approximately every five seconds, after the previous cycle completes. Environmental devices produce temperature and moisture readings, power devices produce wattage, and actuators report their commanded Boolean state.

The simulator uses `http://localhost:5206/` by default. Override `SmartX:SimulatorApiUrl` if the API address changes. The frontend also currently uses port 5206.

`POST /api/Demo/seed` runs an additional HTTP simulation cycle.

## Telemetry processing

1. The API validates the device, metric and typed value.
2. The server classifies priority.
3. An accepted packet enters the standard or urgent queue.
4. A single background worker selects urgent packets before standard packets.
5. Existing generic telemetry processing creates the record and detects anomalies.
6. Timestamp-ordered history is updated.
7. Active faults are raised or resolved using the latest arrival for that device and metric.

POST requests return HTTP 202 with a packet ID and assigned priority. Acceptance means queued; successful completion is observed through history and processor counters.

The queue holds up to 10,000 waiting packets. A full queue returns HTTP 503 with Retry-After. Critical packets do not interrupt work already executing. Equal-priority urgent packets retain arrival order.

Arrival timestamps are preserved. An older normal packet processed after a newer critical packet cannot clear the newer active fault.

### Priority thresholds

| Metric | Standard: 0 | Urgent: 1 | Critical: 2 |
|---|---|---|---|
| Temperature | 5 through 35 degrees C | Below 5 or above 35, within critical boundaries | Below 0 or above 45 |
| Power | At most 4,000 W | Above 4,000 through 6,000 W | Above 6,000 W |
| Moisture | 20 through 100 percent | 10 through below 20 percent | Below 10 percent |

Moisture outside 0-100 percent is rejected. Boolean actuator readings are standard priority. Other valid metric names receive standard priority unless they match a recognised metric.

These are simulation thresholds selected for this implementation.

## Required data structures

| Structure | Implementation | Purpose |
|---|---|---|
| Queue<T> | TelemetryProcessingQueue.cs | Standard FIFO telemetry |
| PriorityQueue<TElement, TPriority> | TelemetryProcessingQueue.cs | Critical and urgent processing before normal backlog |
| Stack<T> | CommandService.cs | Last-in-first-out manual command undo |
| Dictionary<Guid, Sensor> | SensorCollection.cs | Device ID lookup |
| Dictionary<string, Sensor> | SensorCollection.cs | Normalised MAC lookup |
| SortedDictionary<DateTime, List<TelemetryRecord>> | TelemetryHistoryStore.cs | Timestamp-ordered history, retaining timestamp ties |
| Dictionary of device timelines | TelemetryHistoryStore.cs | Locate one device's history |
| HashSet<AlertKey> | ActiveAlertService.cs | Unique active faults per device and fault type |
| Dictionary<ActionPatternKey, LearnedActionPattern> | ActionLearningService.cs | Count learned fault-to-command associations |
| Queue<DeviceSearchObservation> | ActionLearningService.cs | Bounded recent search history |

C# data structure files are under `SmartX.API/DataStructures` and service files under `SmartX.API/Services`.

Dictionary lookups are average O(1). Standard queue and stack operations are amortised O(1). Priority queue operations are O(log n). Sorted-dictionary insertions are O(log n) in the number of timestamp keys. A SortedSet timestamp index supports descending traversal without reversing the entire dictionary. SortedDictionary buckets retain timestamp ties. History retains the newest 10,000 readings by arrival timestamp; raw jagged-array batches are capped at 256. History statistics describe retained readings, while device counters count processed readings.

Part 1 generic packets, operator overloads, jagged arrays, lists and recursive deployment validation remain in the source.

## Manual commands and undo

Only registered actuators accept commands. They initially start OFF.

Successful commands save the previous state and enter the undo stack. Repeating the current state is rejected without creating an undo entry. Undo restores the previous state of the latest command across all actuators.

The simulator reads the same state through HTTP. Commanded states therefore survive subsequent simulation cycles.

The undo stack holds up to 1,000 commands; command-event history retains up to 2,000 entries. The dashboard displays the newest 50 events.

## Active faults and disconnection

A fault is identified by device ID and a normalised fault type. Repeated anomalies update one active alert. A normal reading for the same metric resolves it. A recurring fault can create a new alert.

The connection monitor checks every two seconds. By default, a device with no accepted telemetry for 15 seconds becomes Disconnected. Resumed telemetry restores Online status and resolves its disconnect alert. Configure `SmartX:DisconnectTimeoutSeconds` to adjust the timeout.

Active fault state and historical anomaly records serve different purposes: resolving an alert does not remove its historical readings.

## Learned Suggested Actions

Selecting a device result with non-empty search text records the search and its active faults.

A successful manual command within two minutes can be associated with faults that were active during that search and remain active. One search supplies context to one command. Undo actions do not train the engine.

Suggestions require:

- At least three matching observations.
- An active source fault.
- A registered, connected target actuator.
- A requested state different from its current state.
- A pattern observed within the last seven days.

Conflicting ON/OFF patterns for a target are suppressed. Equivalent target actions are combined. The dashboard displays up to ten suggestions, with their supporting observation count and explanation.

Apply sends an operator-approved command through the normal command endpoint. It appears in history and can be undone. Support counts describe observed behaviour; they are not statistical confidence estimates.

## Reproducible API checks

Keep the API running. Initialise these variables in a test terminal:

```powershell
$ErrorActionPreference = "Stop"
$api = "http://localhost:5206/api"
$devices = Invoke-RestMethod -Uri "$api/Sensor"
$environment = $devices | Where-Object sensorCategory -eq "Environmental" | Select-Object -First 1
$actuator = $devices | Where-Object sensorCategory -eq "Actuator" | Select-Object -First 1
```

### Dictionary lookup

```powershell
Invoke-RestMethod -Uri "$api/Sensor/$($environment.id)"
```

Verify the returned ID matches the selected device. Attempting registration with `aa-bb-cc-dd-ee-01` must reject the duplicate seeded MAC despite formatting differences.

### Critical telemetry and queue status

```powershell
$body = @{
    sensorId = [string]$environment.id
    metric = "Moisture"
    valueType = "float"
    value = 5
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$api/Telemetry" `
    -ContentType "application/json" -Body $body

Invoke-RestMethod -Uri "$api/Telemetry/queue" |
    ConvertTo-Json -Depth 4
```

Expect priority 2 in the acceptance response. The processed count should increase. Inspect history for the reading. The regular simulator can subsequently change the standard Moisture metric.

To verify collection ordering directly, enqueue normal packets followed by critical packets before starting the consumer. Dequeue should return critical packets first, retaining FIFO order within equal priorities and within the standard queue.

### Disconnect and recovery

```powershell
Invoke-RestMethod -Method Post -Uri "$api/Simulation/$($environment.id)/pause"
```

Wait approximately 15-20 seconds. In the dashboard, filter for Disconnected devices and inspect the device. Repeated monitor checks should leave one disconnect alert.

```powershell
Invoke-RestMethod -Method Post -Uri "$api/Simulation/$($environment.id)/resume"
```

Within the next simulation and monitor cycles, the device should return Online and its disconnect alert should clear.

### Commands and undo

```powershell
$state = Invoke-RestMethod -Uri "$api/Command/state/$($actuator.id)"

$body = @{
    sensorId = [string]$actuator.id
    state = -not [bool]$state.state
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$api/Command" `
    -ContentType "application/json" -Body $body

Invoke-RestMethod -Method Post -Uri "$api/Command/undo"
Invoke-RestMethod -Uri "$api/Command/history"
```

Verify undo restores the original state. Avoid concurrent commands during this demonstration because undo is global.

### Predictive dashboard demonstration

1. Submit a low reading with a unique metric such as `Moisture-Demo-1`.
2. Search for Greenhouse Zone A and inspect the result.
3. Change an actuator state and undo it.
4. Repeat the search selection and command/undo pair twice more.
5. Confirm a Suggested Action appears after three matching observations.
6. Apply it; verify the actuator changes state and the satisfied suggestion disappears.
7. Undo, then submit a normal value of 50 for the same test metric.
8. Confirm the test fault and its recommendation clear.

Use a unique metric because automatic simulation updates the ordinary Moisture metric.

### Timeline and navigation

Check Temperature, Moisture and Power graphs, ValveState ON/OFF history, chronological ordering, automatic updates and preserved selections when navigating between modules.

## Repository verification project

Run the committed checks from the repository root:

    dotnet run --project .\SmartX.Verification\SmartX.Verification.csproj

Latest result: all 23 checks passed.

- 14 retention and ordering checks.
- 9 connectivity checks covering accepted arrivals, queue rejection, delayed processing and recovery.

The connectivity checks invoke the controller directly with an HTTP context. Earlier integration checks exercised the running HTTP server.

## Verification completed during development

- Backend and frontend builds.
- Dictionary lookup and normalised duplicate-MAC rejection.
- Timestamp ordering and timestamp-collision handling.
- FIFO, priority, capacity and packet-copy checks.
- Input validation and priority boundaries.
- Command state changes, multi-device undo and event history.
- Unique fault tracking, concurrent duplicate reports, recovery and recurrence.
- HTTP simulation and disconnection/reconnection.
- Search-based learning and relevant suggestion filtering.
- Dashboard search, filters, commands, suggestions and timelines.
- Recursive deployment validation and successful encrypted upload.
- Arrival-order regression: stale normal telemetry cannot clear a newer critical alert.
- Local workload: 320 HTTP readings, including 20 critical readings, stored once with chronological history and no new failures or rejections. Observed submission-to-history time was 0.77 seconds on the development machine. This is a local observation, not a general performance guarantee.

## Current scope and limitations

- Registered devices, telemetry, command state, alerts and learned patterns are in memory and reset on API restart.
- Upload files remain on disk; runtime sensor IDs reset on restart.
- Frontend search context is for a single-operator prototype. Separate operator sessions are not implemented.
- Telemetry history retains up to 10,000 readings across devices. Older readings are removed from global and device indexes. Raw batches are capped at 256.
- Connectivity follows accepted HTTP arrivals. Delayed processing and full-queue rejections do not refresh last-seen activity.
- Polling provides regular updates; WebSockets and physical IoT hardware are not required for this simulation.
- Backend command endpoints simulate state changes; no physical actuator is controlled.
- Graph traversal, mesh routing and tree-based range querying belong to Part 3.
- Docker is not included in the current implementation.

## Evidence and demonstration

Existing Part 1 screenshots are under `SmartX.API/Evidence`.

Part 2 evidence should demonstrate navigation, queue counters, filtering, unique alerts, disconnect recovery, commands and undo, learned Suggested Actions, and ordered timelines.

The lecturer requires a Part 2 video presentation. Recording, upload and the final video link remain outstanding.


## Seeded demonstration dataset

With the API running, execute from the repository root:

    .\scripts\Seed-DemoData.ps1 -Rounds 80

The default run prepares 12 devices and submits 1,280 readings through the HTTP telemetry endpoint:

- Four environmental devices: 640 float readings.
- Four power devices: 320 integer readings.
- Four actuators: 320 Boolean readings.

Existing devices are reused by MAC address. Repeating the script adds another batch of readings.

The dataset includes a critical Moisture reading for Greenhouse Zone B and a critical Power reading for Solar Inverter Room. Select Temperature, Moisture, Power or ValveState in the timeline. Seeded readings and ongoing simulator readings use the same metric names. Subsequent normal readings can resolve faults created by the final seed round.

Avoid issuing actuator commands during seeding because the script captures their states at the start.

Device registrations and readings are held in memory. Run the script again after restarting the API to recreate the dataset. The regular simulator continues updating registered devices.

Seeded telemetry does not fabricate learned recommendations. Suggested Actions still require real search-command observations.

Verification: all 1,280 readings reached history once, with 640 float, 320 integer and 320 Boolean readings. Both expected critical alerts were active without duplicates, and no processing failures were reported.

