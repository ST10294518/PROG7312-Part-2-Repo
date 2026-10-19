Smart - X IoT Mesh Ecosystem
Programming 3B - Advanced Application Development POE
Part 1 - Sensor Data Ingestion and Telemetry

YouTube Link: 
Smart-X is an IoT Mesh Ecosystem application developed for the Programming 3B Advanced Application Development POE. The application provides a web-based dashboard that allows users to register and monitor IoT sensors, submit and retrieve telemetry data, detect telemetry anomalies, validate deployment configurations, and securely attach media and log files to individual sensors. The implementation completed for Part 1 focuses on the Sensor Data Ingestion and Telemetry pillar of the Smart-X ecosystem. The remaining system pillars are intentionally disabled because they are reserved for later POE phases.
System Pillars
The Smart-X dashboard presents three major system pillars.
The first pillar, Sensor Data Ingestion and Telemetry, is active and implemented in Part 1. It provides sensor registration, telemetry ingestion, telemetry monitoring, anomaly detection, sensor status monitoring, telemetry history, media and log uploads, encrypted file storage, and recursive deployment validation.
The second pillar, Real-Time Command Stream and History, is disabled in Part 1 and is reserved for Part 2. Command processing and command-history functionality are therefore not implemented as part of the current Part 1 scope.
The third pillar, Network Topology and Mesh Routing, is also disabled. This functionality is reserved for the final POE phase, where network topology, mesh routing and related optimisation functionality will be implemented.

Technologies Used
The Smart-X backend is developed using C# and .NET 10 with ASP.NET Core Web API. The API uses controllers and Swagger/OpenAPI to expose and document the available endpoints. The frontend is developed using React and Vite, with JavaScript, JSX and CSS used to create the dashboard interface. The interface is responsive and is designed to work across different screen sizes. Other technologies and development tools used include Visual Studio, Developer PowerShell, Node.js, npm, Git and GitHub.

Project Structure
The project consists of two main applications. The SmartX.API project contains the .NET backend. It includes controllers for sensors, telemetry, files, deployment validation, system status and demonstration data. It also contains the models, services and custom data structures required by the application. The SmartX.Client project contains the React frontend. The main user interface is implemented through the React source files, including App.jsx, App.css, index.css and main.jsx. An Evidence folder contains screenshots demonstrating the functionality implemented and tested for Part 1. The solution file is SmartX.sln.
Backend Functionality
The Smart-X API acts as the main backend service for the application. The React client communicates with the API to register sensors, submit telemetry, retrieve telemetry information, upload files and validate deployment configurations. The API provides the following main endpoint groups:
System: The system status endpoint provides information about the Smart-X gateway and identifies the active Part 1 module.
Sensor: The sensor endpoints allow registered sensors to be retrieved and new sensors to be registered.
Telemetry: The telemetry endpoints allow telemetry data to be submitted, retrieved and analysed. Telemetry statistics and sensor-specific telemetry history can also be retrieved.
File: The file upload endpoint allows media, configuration and log files to be attached to a specific sensor.
Deployment: The deployment validation endpoint recursively validates nested deployment configurations.
Demo: The demo endpoint can be used to generate demonstration telemetry data.
Sensor Registration
Smart-X allows users to register IoT sensors through the dashboard. Each sensor contains a unique identifier, MAC address, deployment location, sensor category, status, registration timestamp, last-seen timestamp, total reading count and total alert count. The system supports three sensor categories: Environmental, Power Consumption and Actuator. During registration, the MAC address is validated to ensure that it follows a valid format. Duplicate MAC addresses are also rejected to prevent multiple sensors from being registered with the same hardware identifier.

Telemetry Ingestion
Telemetry is submitted to the Smart-X .NET Web API through the telemetry endpoint. The system supports three telemetry value types: float, integer and Boolean. A generic C# class called TelemetryPacket<T> is used to represent strongly typed telemetry packets. This allows the application to work with different telemetry value types using generic type parameters. The implementation creates and processes telemetry packets for float, integer and Boolean values. Telemetry records contain information including the sensor identifier, metric, value type, value and timestamp. Numeric telemetry can additionally contain a numeric representation, anomaly status and alert information.
Telemetry Simulation
Smart-X contains a background telemetry simulation service that periodically generates demonstration telemetry for registered sensors. Environmental sensors generate values such as temperature and moisture. Power Consumption sensors generate power readings, while Actuator sensors generate Boolean valve-state readings. The simulator allows the dashboard to demonstrate continuous telemetry ingestion and monitoring without requiring physical IoT hardware during development and testing.
Anomaly Detection
The Smart-X system provides dynamic anomaly detection for applicable telemetry values. Temperature values are checked against configured thresholds, power consumption values are checked for unusually high readings, and moisture values are checked for unusually low readings. When an anomaly is detected, the telemetry record is marked as anomalous, an alert message is generated and the corresponding sensor alert count is updated. The dashboard provides visual feedback when anomalies are detected. This functionality supports the engagement strategy selected during the research stage of Part 1: Real-Time Visual Telemetry Feedback with Dynamic Anomaly Alerts.
Sensor Status Monitoring
The application tracks when telemetry was last received from each sensor. Sensors that continue sending telemetry are displayed as Online. If a sensor has not been seen within the configured timeout period, its status can change to Disconnected. This provides the operator with visual information about the availability of registered sensors.
 
C# Data Structures
Part 1 demonstrates several required C# data-structure concepts. The SensorCollection class manages registered sensors using a List<Sensor> internally. It provides functionality for adding, removing and locating sensors. The TelemetryHistoryStore manages telemetry history using a list of telemetry records. It also uses jagged arrays for raw numeric telemetry batches before the optimised list-based history is used for ongoing access. The implementation therefore demonstrates both the required sequential telemetry storage approach and the subsequent use of List<T> for efficient history management.

Generic Programming
The application uses the generic class TelemetryPacket<T> to support different telemetry value types.
The implementation supports:
TelemetryPacket<float>
TelemetryPacket<int>
TelemetryPacket<bool>
This allows telemetry packets to maintain their specific value types rather than using a single non-generic telemetry representation.
Operator Overloading
The Smart-X telemetry implementation includes operator overloading for numeric telemetry operations. The overloaded operators support addition, subtraction, greater-than comparisons and less-than comparisons. These operations are used during telemetry aggregation, delta calculations, deviation calculations and threshold comparisons for anomaly detection.
Recursive Deployment Validation
Smart-X implements recursive validation of nested deployment configurations. Deployment configurations are represented using DeploymentNode objects. Each deployment node can contain child nodes, allowing the configuration to represent a nested deployment structure. The DeploymentValidator recursively checks each node in the deployment tree. The validation process first checks the current node and then recursively checks each child node. A clear base case is included for a null node. This allows the application to determine whether an entire nested deployment configuration is correctly configured. Both valid and invalid nested deployment configurations were tested during Part 1.
Media and Log Upload
Smart-X provides functionality for attaching files to individual sensor profiles. The upload functionality uses multipart form data and supports configuration files, log files and supported image formats. The supported file types include JSON, TXT, LOG, CSV, XML, JPG, JPEG and PNG files. The application validates uploaded files before they are stored. A maximum file size of 10 MB is enforced. Files are stored within sensor-specific directories so that uploaded files can be associated with the appropriate sensor.
File Encryption
Uploaded files are encrypted before they are stored. The application uses AES encryption and generates a unique encrypted filename for uploaded files. The encryption key is stored using .NET User Secrets rather than being stored directly in appsettings.json. This helps prevent sensitive encryption configuration from being committed to the source-control repository.
Security Features
The Part 1 implementation includes several security controls. HTTPS redirection is configured for API communication. CORS is configured to control communication between the React client and the backend API.Input validation is performed when registering sensors and submitting files. MAC addresses are validated and duplicate sensor identifiers are prevented.Uploaded files are checked for valid extensions and maximum file size before processing. Uploaded files are encrypted before being stored. The encryption key is maintained using .NET User Secrets instead of being placed directly into the application's configuration file.
React Dashboard
The Smart-X React dashboard provides the main user interface for interacting with the system. The dashboard displays the Smart-X gateway status and clearly identifies the active Part 1 module. The dashboard provides sections for sensor registration, telemetry ingestion, sensor monitoring, telemetry statistics, anomaly alerts, media and log uploads and deployment validation. The interface also displays the three Smart-X system pillars. The Part 1 pillar is shown as active, while the Part 2 and final POE pillars are shown as disabled. The frontend has been designed using responsive CSS so that the dashboard can adapt to different screen sizes.




Swagger API Documentation

Swagger/OpenAPI is included in the Smart-X backend to provide interactive API documentation.
After starting the API, Swagger can be accessed at: http://localhost:5206/swagger 
Swagger can be used to view and test the available API endpoints, including sensor registration, telemetry submission and retrieval, file uploads, deployment validation and system status.
Installation Requirements
The following software is required to run the Smart-X application:
.NET 10 SDK
Node.js
npm
Visual Studio with .NET 10 support
Git

The installed .NET version can be checked using dotnet --version.
The installed Node.js version can be checked using node --version.
The installed npm version can be checked using npm --version.

Running the Backend
Open Developer PowerShell and navigate to the SmartX.API directory.
Run dotnet restore to restore the required .NET dependencies.
Run dotnet build to build the API.
Run dotnet run to start the backend application.
The API is available locally at: http://localhost:5206 
Swagger is available at: http://localhost:5206/swagger 


Running the Frontend
Open a second Developer PowerShell terminal and navigate to the SmartX.Client directory.
Run npm install to install the required frontend dependencies.
Run npm run dev to start the Vite development server.
The React application is available at: http://localhost:5173
Open this address in a web browser to access the Smart-X dashboard.

Production Build
The React application can be tested using a production build. From the SmartX.Client directory, run npm run build. A successful build generates the dist directory containing the production frontend files. The .NET API can be cleaned and rebuilt using dotnet clean followed by dotnet build. The final Part 1 implementation was successfully tested using these build commands.
Evidence
The Evidence folder contains screenshots demonstrating the implementation and testing of the major Part 1 functionality.
The evidence includes the Smart-X startup interface and three system pillars, sensor registration, float telemetry and anomaly detection, integer telemetry, Boolean telemetry, valid recursive deployment validation, invalid recursive deployment validation, media upload and encryption, Swagger API endpoints and system status. These screenshots provide practical evidence of the functionality implemented for Part 1.
Part 1 Scope
The completed Part 1 implementation focuses specifically on Sensor Data Ingestion and Telemetry. The completed functionality includes sensor registration, sensor categorisation, telemetry ingestion, generic telemetry processing, float/integer/Boolean telemetry, telemetry history, operator overloading, jagged arrays, list-based data management, recursive deployment validation, anomaly detection, sensor status monitoring, multipart media and log uploads, encrypted file storage, Swagger API documentation and a responsive React dashboard. The Real-Time Command Stream and History functionality is intentionally reserved for Part 2. The Network Topology and Mesh Routing functionality is intentionally reserved for the final POE phase. This ensures that the current implementation remains within the required Part 1 scope while establishing the foundation for the later Smart-X development phases.
Conclusion
Smart-X Part 1 provides the foundation for the IoT Mesh Ecosystem by implementing the required Sensor Data Ingestion and Telemetry functionality. The combination of a React frontend and .NET Web API provides a functional client-server architecture for interacting with IoT sensor data. The implementation demonstrates sensor management, generic telemetry processing, anomaly detection, telemetry history, operator overloading, recursive validation, secure file handling and responsive dashboard functionality. The application has been built and tested successfully for both the backend and frontend components.
