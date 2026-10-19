# Smart-X Part 2 Verification

Verification date: 19 October 2026.

These results were observed during development on the local Windows environment. They describe the tested scenarios and do not guarantee performance under every workload.

## Environment

- .NET SDK 10.0.401
- Node.js 26.8.1
- npm 11.19.0
- API: http://localhost:5206
- Frontend: http://localhost:5173

## Results

| Area | Verification | Result |
|---|---|---|
| Builds | Backend and frontend production builds | Passed |
| Frontend lint | Latest oxlint run | 0 warnings, 0 errors |
| Dependencies | Latest npm audit | 0 vulnerabilities |
| Device dictionaries | ID lookup and differently formatted duplicate MAC | Passed |
| Ordered history | Ordering, timestamp ties, device filtering and snapshots | 8 checks passed |
| Queue collections | FIFO, critical precedence, equal-priority ordering, capacity and input copying | 9 checks passed |
| Input validation | Threshold boundaries, types, invalid values and devices | 34 checks passed |
| Commands | State changes, multiple-device undo and command history | 12 checks passed |
| Active alerts | Duplicate prevention, concurrent reports, recovery and recurrence | 9 checks passed |
| Learning | Repeated behaviour, duplicate commands, expired searches and unrelated faults | 10 checks passed |
| Recommendations | Support threshold, relevance, target state, conflicts and stale patterns | 11 checks passed |
| Arrival ordering | Older normal packets cannot clear newer faults; history retains arrival times | 6 checks passed |
| HTTP ingestion | Accepted packet reached background processing and history | Passed |
| HTTP simulation | Automatic cycles and manual demo cycles use queued ingestion | Passed |
| Disconnection | Pause, one disconnect alert, repeated monitoring and recovery | Passed |
| Dashboard search | Query, category/condition filters, selection and alert inspection | Passed |
| Dashboard commands | ON/OFF, retained state, undo and visible command history | Passed |
| Dashboard suggestions | Learned suggestion, Apply and undo | Passed |
| Dashboard timelines | Temperature, moisture, power, Boolean history, ordering and live updates | Passed |
| Navigation | Selections remain when switching dashboard views | Passed in reported UI checks |
| Part 1 recursion | Valid deployment accepted; invalid nested child detected | Passed |
| Part 1 uploads | JSON attachment uploaded successfully with encryption | Passed |

## Local workload observation

A test submitted 320 telemetry readings through HTTP:

- 300 standard readings.
- 20 critical readings.
- All requests accepted with their expected priorities.
- All 320 readings stored once.
- Exactly 20 anomaly records found.
- History remained chronologically ordered.
- No new processing failures or queue rejections.
- Observed submission-to-history duration: 0.77 seconds.
- A normal recovery reading was submitted afterward.

The priority collection ordering was checked separately. The workload timing alone does not prove priority ordering or establish a general throughput guarantee.

## Screenshot evidence

Useful supporting screenshots can be saved in this folder:

1. Navigation and live processing counters.
2. Device search, filters and selected-device alerts.
3. Actuator state and command/undo history.
4. Suggested Action with its supporting observations and explanation.
5. Successful application of a suggestion.
6. Numeric timeline and ordered history.
7. Disconnected device and its single active alert.
8. Final build, lint and audit results.

Screenshot files should show actual application results. The assessment brief separately requires a video or presentation according to the lecturer's instructions.

## Scope notes

- Runtime device data, telemetry, commands and learned patterns reset when the API restarts.
- Search context is shared in this single-operator prototype.
- Telemetry history retains the newest 10,000 readings; raw numeric batches are capped at 256.
- Connectivity follows accepted HTTP arrivals. Processing old queued packets cannot falsely reconnect a device.
- Part 3 network topology and routing remain reserved for the final PoE.
- Earlier isolated checks used temporary console projects. SmartX.Verification now contains 23 committed retention and connectivity checks, all passing.


## Additional verification after source review

All 23 committed repository checks passed: 14 retention checks and 9 connectivity checks.

Run from the repository root:

    dotnet run --project .\SmartX.Verification\SmartX.Verification.csproj

The lecturer requires a video presentation. Recording and the final video link remain outstanding.

## Seeded dataset verification

The Seed-DemoData.ps1 script was executed with 80 rounds.

- 12 distinct demo devices, including nine newly registered devices.
- 1,280 HTTP telemetry requests accepted.
- All 1,280 readings stored with unique record IDs.
- 640 float, 320 integer and 320 Boolean readings.
- One expected moisture demo alert and one expected power demo alert.
- Background processor reported zero failures.
- Queue status at verification: zero pending and zero rejected packets.

The script recreates the in-memory demonstration dataset after API restart.

## Standard metric integration

The seed script now uses Temperature, Moisture, Power and ValveState, matching the regular simulator.

The timeline excludes older metric names ending in -Demo from its selector. Existing records remain in backend history.

Frontend lint and production build passed. Dashboard checks confirmed standard metrics display correctly, separate -Demo options are removed, and readings continue updating.
