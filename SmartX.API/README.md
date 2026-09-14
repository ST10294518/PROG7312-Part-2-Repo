Absolutely. Here is **one complete `README.md` file**, ready to paste directly into your blank README. I’ve kept it focused on **Part 1**, based on your actual Smart-X implementation, without adding unnecessary Part 2 functionality.

````markdown
# Smart-X IoT Mesh Ecosystem

## Programming 3B – Advanced Application Development POE

### Part 1 – Sensor Data Ingestion and Telemetry

Smart-X is an IoT Mesh Ecosystem application developed for the Programming 3B Advanced Application Development POE.

The purpose of the Smart-X system is to provide a web-based IoT dashboard that allows users to register sensors, receive and process telemetry data, monitor sensor activity, detect anomalies, validate deployment configurations, and securely attach media or log files to individual sensors.

Part 1 focuses on the **Sensor Data Ingestion and Telemetry** pillar of the Smart-X system.

The remaining system pillars are intentionally disabled because they are implemented in later POE phases.

---

# 1. Part 1 System Pillars

The Smart-X dashboard contains three system pillars.

### Active – Part 1

**Sensor Data Ingestion & Telemetry**

This is the active pillar implemented in Part 1.

It provides:

- Sensor registration
- Sensor identification
- Deployment location management
- Sensor category management
- Telemetry ingestion
- Telemetry history
- Float, integer and Boolean telemetry
- Anomaly detection
- Sensor online/disconnected monitoring
- Media and log uploads
- Encrypted file storage
- Recursive deployment validation
- API integration
- Responsive dashboard

### Disabled – Part 2

**Real-Time Command Stream & History**

This functionality is intentionally disabled and reserved for Part 2.

### Disabled – Final POE

**Network Topology & Mesh Routing**

This functionality is intentionally disabled and reserved for the final POE phase.

---

# 2. Technologies Used

## Backend

- C#
- .NET 10
- ASP.NET Core Web API
- ASP.NET Core Controllers
- Swagger / OpenAPI
- Background Services
- Generic C# classes
- Operator overloading
- Lists
- Jagged arrays
- Recursive validation

## Frontend

- React
- Vite
- JavaScript
- JSX
- CSS
- Responsive Web Design

## Development Tools

- Visual Studio
- Developer PowerShell
- Node.js
- npm
- Git
- GitHub

---

# 3. Project Structure

```text
SmartX/
│
├── SmartX.API/
│   │
│   ├── Controllers/
│   │   ├── DemoController.cs
│   │   ├── DeploymentController.cs
│   │   ├── FileController.cs
│   │   ├── SensorController.cs
│   │   ├── SystemController.cs
│   │   └── TelemetryController.cs
│   │
│   ├── DataStructures/
│   │   ├── SensorCollection.cs
│   │   └── TelemetryHistoryStore.cs
│   │
│   ├── Models/
│   │   ├── DeploymentNode.cs
│   │   ├── Sensor.cs
│   │   ├── TelemetryInput.cs
│   │   ├── TelemetryPacket.cs
│   │   └── TelemetryRecord.cs
│   │
│   ├── Services/
│   │   ├── DeploymentValidator.cs
│   │   ├── FileService.cs
│   │   ├── SensorService.cs
│   │   ├── TelemetryService.cs
│   │   └── TelemetrySimulationService.cs
│   │
│   ├── Uploads/
│   ├── Program.cs
│   ├── SmartX.API.csproj
│   ├── appsettings.json
│   └── Properties/
│
├── SmartX.Client/
│   │
│   ├── src/
│   │   ├── App.jsx
│   │   ├── App.css
│   │   ├── index.css
│   │   └── main.jsx
│   │
│   ├── package.json
│   ├── package-lock.json
│   └── vite.config.js
│
├── Evidence/
│
├── SmartX.sln
└── README.md
````

---

# 4. Prerequisites

The following software is required to run the project:

* .NET 10 SDK
* Node.js
* npm
* Visual Studio with .NET 10 support
* Git

Check the installed .NET version:

```powershell
dotnet --version
```

Check the installed Node.js version:

```powershell
node --version
```

Check the installed npm version:

```powershell
npm --version
```

---

# 5. Clone the Repository

Clone the GitHub repository:

```powershell
git clone <YOUR-GITHUB-REPOSITORY-URL>
```

Navigate into the project directory:

```powershell
cd SmartX
```

Open the solution:

```text
SmartX.sln
```

---

# 6. Backend Setup

Open a Developer PowerShell terminal.

Navigate to the API project:

```powershell
cd SmartX.API
```

Restore the .NET dependencies:

```powershell
dotnet restore
```

Build the API:

```powershell
dotnet build
```

A successful build should display:

```text
Build succeeded.
```

---

# 7. Configure User Secrets

The Smart-X API uses .NET User Secrets to store the encryption key used for encrypted sensor file uploads.

This prevents the encryption key from being stored directly in `appsettings.json`.

From the `SmartX.API` directory, initialise User Secrets if required:

```powershell
dotnet user-secrets init
```

Set the encryption key:

```powershell
dotnet user-secrets set "SmartX:EncryptionKey" "U2hhcmVkU21hcnRYRGV2ZWxvcG1lbnRLZXkxMjM0NTY="
```

The maximum file upload size remains configured in:

```text
SmartX.API/appsettings.json
```

Example:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "SmartX": {
    "MaxUploadSizeMB": 10
  }
}
```

The encryption key should not be committed to GitHub.

---

# 8. Run the Backend API

From the `SmartX.API` directory, run:

```powershell
dotnet run
```

The API runs locally at:

```text
http://localhost:5206
```

Swagger is available at:

```text
http://localhost:5206/swagger
```

The API provides the backend services used by the React dashboard.

---

# 9. Frontend Setup

Open a second Developer PowerShell terminal.

Navigate to the React client:

```powershell
cd SmartX\SmartX.Client
```

Install the required npm packages:

```powershell
npm install
```

Run the React development server:

```powershell
npm run dev
```

The frontend runs locally at:

```text
http://localhost:5173
```

Open the following address in a web browser:

```text
http://localhost:5173
```

---

# 10. Frontend Production Build

The React application can be tested using a production build.

From the `SmartX.Client` directory, run:

```powershell
npm run build
```

A successful build generates the `dist` directory.

Example:

```text
dist/
├── index.html
└── assets/
```

---

# 11. API Endpoints

The Smart-X API exposes the following main endpoints.

## System

```text
GET /api/System/status
```

Returns the current Smart-X gateway status and identifies the active Part 1 module.

---

## Sensor

```text
GET /api/Sensor
GET /api/Sensor/{id}
POST /api/Sensor
```

These endpoints are used to retrieve and register sensors.

---

## Telemetry

```text
GET /api/Telemetry
GET /api/Telemetry/statistics
GET /api/Telemetry/sensor/{sensorId}
POST /api/Telemetry
```

These endpoints are used to submit, retrieve and analyse telemetry data.

---

## File Upload

```text
POST /api/File/sensor/{sensorId}
```

This endpoint allows media, configuration and log files to be attached to a specific sensor.

Files are validated and encrypted before being stored.

---

## Deployment Validation

```text
POST /api/Deployment/validate
```

This endpoint validates nested deployment configurations using recursive validation.

---

## Demo

```text
POST /api/Demo/seed
```

This endpoint generates demonstration telemetry data.

---

# 12. Sensor Registration

Sensors can be registered through the Smart-X dashboard.

Each sensor contains:

* Unique ID
* MAC address
* Deployment location
* Sensor category
* Status
* Registration timestamp
* Last-seen timestamp
* Total readings
* Total alerts

The supported sensor categories are:

```text
Environmental
Power Consumption
Actuator
```

MAC addresses are validated during registration.

Duplicate MAC addresses are also rejected.

---

# 13. Telemetry Ingestion

The Smart-X system receives telemetry through the .NET Web API.

The system supports three telemetry value types:

```text
Float
Int
Bool
```

The implementation uses the generic C# class:

```csharp
TelemetryPacket<T>
```

This allows strongly typed telemetry packets to be created for different value types.

Examples include:

```csharp
TelemetryPacket<float>
TelemetryPacket<int>
TelemetryPacket<bool>
```

Telemetry records contain information such as:

* Sensor ID
* Metric
* Value
* Value type
* Timestamp
* Numeric value where applicable
* Anomaly status
* Alert message

---

# 14. Telemetry Simulation

The Smart-X API includes a background telemetry simulation service.

The simulator periodically generates telemetry for the registered sensors.

Examples include:

### Environmental Sensor

```text
Temperature
Moisture
```

### Power Consumption Sensor

```text
Power
```

### Actuator

```text
ValveState
```

This allows the dashboard to demonstrate continuous telemetry ingestion and monitoring.

---

# 15. Anomaly Detection

Smart-X performs anomaly detection on applicable telemetry values.

Examples of anomaly rules include:

* Temperature above the configured threshold
* Temperature below the configured threshold
* High power consumption
* Low moisture

When an anomaly is detected:

1. The telemetry record is marked as anomalous.
2. An alert message is generated.
3. The sensor alert count is updated.
4. The dashboard displays the anomaly.

The implementation supports the Part 1 engagement strategy selected during the research stage:

**Real-Time Visual Telemetry Feedback with Dynamic Anomaly Alerts**

---

# 16. Sensor Status Monitoring

The system tracks the last time telemetry was received from each sensor.

Sensors that continue sending telemetry are displayed as:

```text
● Online
```

If a sensor has not been seen within the configured timeout period, its status can change to:

```text
⚠ Disconnected
```

This provides visual feedback about sensor availability.

---

# 17. Telemetry History and Data Structures

Telemetry history is managed using:

```csharp
TelemetryHistoryStore
```

The implementation uses:

```text
List<TelemetryRecord>
```

for optimised telemetry history management.

Jagged arrays are also used to store raw numeric telemetry batches.

The system provides functionality for:

* Adding telemetry records
* Retrieving the latest telemetry
* Retrieving sensor-specific history
* Tracking telemetry record counts
* Tracking raw telemetry batches

---

# 18. Operator Overloading

The Smart-X implementation includes operator overloading for numeric telemetry operations.

The overloaded operators support:

```text
+
-
>
<
```

These operators are used for telemetry operations including:

* Aggregation
* Delta calculations
* Deviation calculations
* Threshold comparisons
* Anomaly detection

This demonstrates the required use of C# operator overloading within the telemetry processing logic.

---

# 19. Recursive Deployment Validation

Smart-X implements recursive validation using:

```text
DeploymentValidator
```

Deployment configurations are represented as nested:

```text
DeploymentNode
```

objects.

The validator checks whether the current node is configured and then recursively validates its child nodes.

The validation process includes:

1. Checking the current deployment node.
2. Checking whether the node is configured.
3. Recursively processing each child.
4. Returning the final validation result.

The recursive method includes a clear base case.

This allows nested deployment/configuration trees to be validated.

---

# 20. Media and Log Upload

The Smart-X dashboard supports attaching files to specific sensor profiles.

The upload uses multipart form data.

Supported file extensions include:

```text
.json
.txt
.log
.csv
.xml
.jpg
.jpeg
.png
```

The system applies file validation before storing uploaded files.

The upload system also enforces a maximum file size of:

```text
10 MB
```

Files are stored in a sensor-specific directory.

---

# 21. File Encryption

Uploaded files are encrypted before being stored.

The encryption key is stored using .NET User Secrets rather than directly inside `appsettings.json`.

Encrypted files are stored using generated filenames rather than the original filename.

The upload process therefore provides:

* File type validation
* File size validation
* Sensor-specific storage
* Encrypted storage
* Randomised file naming
* Protected encryption configuration

---

# 22. Security Features

The Smart-X Part 1 implementation includes several security measures.

### HTTPS

The API is configured for HTTPS redirection.

### CORS

The API uses a configured CORS policy to control communication between the React client and API.

### Input Validation

User input is validated before processing.

### Sensor Validation

MAC addresses are validated and duplicate sensors are prevented.

### File Validation

Uploaded files are checked for:

* Supported extensions
* Maximum file size

### Encrypted Storage

Uploaded files are encrypted before being stored.

### User Secrets

Sensitive encryption configuration is stored using .NET User Secrets.

---

# 23. React Dashboard

The React dashboard provides the user interface for the Smart-X system.

The dashboard includes:

* Smart-X gateway status
* Part 1 active module indicator
* Disabled future system pillars
* Sensor overview
* Sensor registration
* Telemetry submission
* Telemetry statistics
* Anomaly alerts
* Sensor status
* Media/log uploads
* Deployment validation

The interface also uses responsive CSS to support different screen sizes.

---

# 24. Swagger / OpenAPI

Swagger is included to provide interactive API documentation.

After starting the API, open:

```text
http://localhost:5206/swagger
```

Swagger allows the available Smart-X API endpoints to be viewed and tested.

It can be used to test:

* Sensor registration
* Sensor retrieval
* Telemetry submission
* Telemetry retrieval
* Telemetry statistics
* File upload
* Deployment validation
* System status
* Demo telemetry generation

---

# 25. Evidence

The `Evidence` folder contains screenshots demonstrating the implementation and testing of the Part 1 functionality.

The evidence includes:

```text
Evidence/
│
├── Evidence_01_Startup_Three_Pillars.png
├── Evidence_02_Sensor_Registration.png
├── Evidence_03_Telemetry_Float_Anomaly.png
├── Evidence_04_Integer_Telemetry.png
├── Evidence_05_Boolean_Telemetry.png
├── Evidence_06_Recursive_Validation_Valid.png
├── Evidence_07_Recursive_Validation_Invalid.png
├── Evidence_08_Media_Upload_Encryption.png
├── Evidence_09_Swagger_API_Endpoints.png
└── Evidence_10_System_Status.png
```

These screenshots provide practical evidence of the major Part 1 implementation features.

---

# 26. Build Verification

The Smart-X API has been tested using:

```powershell
dotnet clean
dotnet build
```

The API successfully completed the clean and build process.

The React client has been tested using:

```powershell
npm run build
```

The React production build successfully completed and generated the `dist` directory.

---

# 27. Running the Complete Application

Two terminals are required.

## Terminal 1 – Backend API

Navigate to:

```powershell
cd SmartX\SmartX.API
```

Run:

```powershell
dotnet run
```

API:

```text
http://localhost:5206
```

Swagger:

```text
http://localhost:5206/swagger
```

---

## Terminal 2 – React Client

Navigate to:

```powershell
cd SmartX\SmartX.Client
```

Run:

```powershell
npm run dev
```

Client:

```text
http://localhost:5173
```

Open:

```text
http://localhost:5173
```

in a web browser.

---

# 28. Part 1 Completion

Part 1 establishes the Sensor Data Ingestion and Telemetry foundation of the Smart-X IoT Mesh Ecosystem.

The completed implementation provides:

* A staged three-pillar Smart-X interface
* Sensor registration
* Sensor categorisation
* Telemetry ingestion
* Generic telemetry processing
* Float, integer and Boolean telemetry
* Telemetry history
* Operator overloading
* Jagged arrays
* List-based data management
* Recursive deployment validation
* Dynamic anomaly detection
* Sensor status monitoring
* Multipart media/log upload
* Encrypted file storage
* Swagger API documentation
* Responsive React dashboard
* Security controls

The following functionality remains intentionally disabled until the relevant POE phases:

```text
Part 2:
Real-Time Command Stream & History

Final POE:
Network Topology & Mesh Routing
```

The current Part 1 implementation therefore provides the required foundation for the later Smart-X IoT Mesh Ecosystem development phases.

---

# Author

**Programming 3B – Advanced Application Development**

**Smart-X IoT Mesh Ecosystem**

**Part 1 – Sensor Data Ingestion and Telemetry**

````

### One important thing before you commit it

At the top of the README, replace:

```text
<YOUR-GITHUB-REPOSITORY-URL>
````

with your **actual GitHub repository URL**.

Everything else can be pasted as-is.

After you save it, **don't commit yet**. The next thing we'll do is check the README and your project structure together, then we can move toward the final Part 1 submission audit.
