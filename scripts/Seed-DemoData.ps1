param(
    [ValidateRange(1, 100)]
    [int]$Rounds = 80,

    [string]$ApiUrl = "http://localhost:5206/api"
)

$ErrorActionPreference = "Stop"
$api = $ApiUrl.TrimEnd("/")
Add-Type -AssemblyName System.Net.Http

$specifications = @(
    @{ Mac = "AA:BB:CC:DD:EE:01"; Location = "Greenhouse Zone A"; Category = "Environmental" }
    @{ Mac = "AA:BB:CC:DD:EE:02"; Location = "Utility Room"; Category = "Power Consumption" }
    @{ Mac = "AA:BB:CC:DD:EE:03"; Location = "Control Room"; Category = "Actuator" }

    @{ Mac = "02:73:12:20:00:01"; Location = "Greenhouse Zone B"; Category = "Environmental" }
    @{ Mac = "02:73:12:20:00:02"; Location = "Hydroponics Zone C"; Category = "Environmental" }
    @{ Mac = "02:73:12:20:00:03"; Location = "Nursery Zone D"; Category = "Environmental" }

    @{ Mac = "02:73:12:20:00:04"; Location = "Solar Inverter Room"; Category = "Power Consumption" }
    @{ Mac = "02:73:12:20:00:05"; Location = "Irrigation Power Supply"; Category = "Power Consumption" }
    @{ Mac = "02:73:12:20:00:06"; Location = "Cold Storage Meter"; Category = "Power Consumption" }

    @{ Mac = "02:73:12:20:00:07"; Location = "Greenhouse Pump B"; Category = "Actuator" }
    @{ Mac = "02:73:12:20:00:08"; Location = "Hydroponics Relay C"; Category = "Actuator" }
    @{ Mac = "02:73:12:20:00:09"; Location = "Nursery Valve D"; Category = "Actuator" }
)

$existing = Invoke-RestMethod -Uri "$api/Sensor"
$devicesByMac = @{}

foreach ($device in $existing) {
    $devicesByMac[$device.macAddress.ToUpperInvariant()] = $device
}

$devices = @()
$created = 0

foreach ($spec in $specifications) {
    $device = $devicesByMac[$spec.Mac]

    if (-not $device) {
        $json = @{
            macAddress = $spec.Mac
            deploymentLocation = $spec.Location
            sensorCategory = $spec.Category
        } | ConvertTo-Json -Compress

        $device = Invoke-RestMethod -Method Post -Uri "$api/Sensor" `
            -ContentType "application/json" -Body $json

        $created++
    }

    if ($device.sensorCategory -ne $spec.Category) {
        throw "Existing device $($spec.Mac) has an unexpected category."
    }

    $devices += $device
}

Write-Host "Demo devices ready: $($devices.Count). Newly registered: $created."

# Preserve existing actuator states during telemetry seeding.
$actuatorStates = @{}

foreach ($device in $devices) {
    if ($device.sensorCategory -eq "Actuator") {
        $state = Invoke-RestMethod `
            -Uri "$api/Command/state/$($device.id)"

        $actuatorStates[[string]$device.id] = [bool]$state.state
    }
}

$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(20)

function Send-Reading {
    param(
        [string]$SensorId,
        [string]$Metric,
        [string]$ValueType,
        [object]$Value
    )

    $json = @{
        sensorId = $SensorId
        metric = $Metric
        valueType = $ValueType
        value = $Value
    } | ConvertTo-Json -Compress

    $content = [System.Net.Http.StringContent]::new(
        $json,
        [System.Text.Encoding]::UTF8,
        "application/json"
    )

    $response = $null

    try {
        $response = $client.PostAsync(
            "$api/Telemetry", $content
        ).GetAwaiter().GetResult()

        $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()

        if ([int]$response.StatusCode -ne 202) {
            throw "Seed request failed: HTTP $([int]$response.StatusCode). $text"
        }
    }
    finally {
        if ($null -ne $response) { $response.Dispose() }
        $content.Dispose()
    }
}

$accepted = 0

try {
    for ($round = 0; $round -lt $Rounds; $round++) {
        for ($index = 0; $index -lt $devices.Count; $index++) {
            $device = $devices[$index]
            $sensorId = [string]$device.id
            $phase = ($round + $index) / 6.0

            switch ($device.sensorCategory) {
                "Environmental" {
                    $temperature = [single][Math]::Round(
                        24 + 5 * [Math]::Sin($phase), 2
                    )

                    $moisture = [single][Math]::Round(
                        50 + 15 * [Math]::Cos($phase), 2
                    )

                    # Submit a critical example on the final round for Zone B.
                    if ($round -eq ($Rounds - 1) -and
                        $device.macAddress -eq "02:73:12:20:00:01") {
                        $moisture = [single]5
                    }

                    Send-Reading $sensorId "Temperature" "float" $temperature
                    Send-Reading $sensorId "Moisture" "float" $moisture
                    $accepted += 2
                }

                "Power Consumption" {
                    $power = [int][Math]::Round(
                        1800 + 700 * [Math]::Sin($phase)
                    )

                    # Submit a critical power example on the final round.
                    if ($round -eq ($Rounds - 1) -and
                        $device.macAddress -eq "02:73:12:20:00:04") {
                        $power = 6500
                    }

                    Send-Reading $sensorId "Power" "int" $power
                    $accepted++
                }

                "Actuator" {
                    Send-Reading $sensorId "ValveState" "bool" `
                        $actuatorStates[$sensorId]
                    $accepted++
                }
            }
        }

        if (($round + 1) % 10 -eq 0 -or $round -eq ($Rounds - 1)) {
            Write-Host "Completed $($round + 1)/$Rounds rounds; $accepted readings accepted."
        }
    }
}
finally {
    $client.Dispose()
}

Write-Host ""
Write-Host "Seed submission complete: $accepted readings accepted."
Write-Host "Background processing may still be finishing."
Write-Host "Inspect Greenhouse Zone B for Moisture."
Write-Host "Inspect Solar Inverter Room for Power."
Write-Host "Select Temperature, Moisture, Power or ValveState in the timeline."
Write-Host "Repeating this script reuses devices and adds another reading batch."
Write-Host "Runtime demo data resets when the API restarts."

