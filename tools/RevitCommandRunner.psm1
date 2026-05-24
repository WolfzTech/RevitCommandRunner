# RevitCommandRunner - AI Helper Module
# PowerShell module for AI agents to interact with RevitCommandRunner

$ErrorActionPreference = "Stop"

# Default paths
$script:DefaultQueuePath = "$env:LOCALAPPDATA\RevitCommandRunner\command-queue.json"
$script:DefaultResultsDir = "$env:LOCALAPPDATA\RevitCommandRunner\results"

<#
.SYNOPSIS
    Execute a Revit command and wait for results.

.DESCRIPTION
    Writes a command request to the queue file, waits for Revit to execute it,
    and returns the result. This is the main function AI agents should use.

.PARAMETER DllPath
    Full path to the DLL containing the command.

.PARAMETER CommandClassName
    Full class name of the command (e.g., "MyNamespace.MyCommand").

.PARAMETER Args
    Optional array of string arguments to pass to the command.

.PARAMETER TimeoutSeconds
    Maximum time to wait for results (default: 300 seconds / 5 minutes).

.PARAMETER QueuePath
    Override the default queue file path.

.PARAMETER ResultsDir
    Override the default results directory.

.EXAMPLE
    Invoke-RevitCommand -DllPath "D:\MyPlugin\bin\Debug\MyPlugin.dll" -CommandClassName "MyPlugin.VerifyCommand"

.EXAMPLE
    $result = Invoke-RevitCommand -DllPath "D:\MyPlugin\bin\Debug\MyPlugin.dll" -CommandClassName "MyPlugin.VerifyCommand" -Args @("arg1", "arg2")
    if ($result.success) {
        Write-Host "Command succeeded: $($result.message)"
    } else {
        Write-Host "Command failed: $($result.message)"
    }
#>
function Invoke-RevitCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string]$DllPath,

        [Parameter(Mandatory=$true)]
        [string]$CommandClassName,

        [Parameter(Mandatory=$false)]
        [string[]]$Args = @(),

        [Parameter(Mandatory=$false)]
        [int]$TimeoutSeconds = 300,

        [Parameter(Mandatory=$false)]
        [string]$QueuePath = $script:DefaultQueuePath,

        [Parameter(Mandatory=$false)]
        [string]$ResultsDir = $script:DefaultResultsDir
    )

    # Validate DLL exists
    if (-not (Test-Path $DllPath)) {
        throw "DLL not found: $DllPath"
    }

    # Generate unique ID
    $id = "run-$(Get-Date -Format 'yyyyMMddHHmmssfff')"

    # Create command request
    $request = @{
        id = $id
        timestamp = (Get-Date).ToUniversalTime().ToString("o")
        dll = $DllPath
        command = $CommandClassName
        args = $Args
        metadata = @{
            source = "PowerShell"
            user = $env:USERNAME
        }
    }

    # Ensure directories exist
    $queueDir = Split-Path $QueuePath -Parent
    if (-not (Test-Path $queueDir)) {
        New-Item -ItemType Directory -Path $queueDir -Force | Out-Null
    }
    if (-not (Test-Path $ResultsDir)) {
        New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
    }

    # Write request to queue file
    Write-Verbose "Writing command request to: $QueuePath"
    $request | ConvertTo-Json -Depth 10 | Set-Content -Path $QueuePath -Encoding UTF8

    # Wait for result file
    $resultPath = Join-Path $ResultsDir "results-$id.json"
    Write-Verbose "Waiting for result file: $resultPath"

    $startTime = Get-Date
    $found = $false

    while (((Get-Date) - $startTime).TotalSeconds -lt $TimeoutSeconds) {
        if (Test-Path $resultPath) {
            $found = $true
            break
        }
        Start-Sleep -Milliseconds 500
    }

    if (-not $found) {
        throw "Timeout waiting for command result after $TimeoutSeconds seconds. Result file not found: $resultPath"
    }

    # Read and parse result
    Start-Sleep -Milliseconds 200  # Brief wait to ensure file write is complete
    $resultJson = Get-Content -Path $resultPath -Raw -Encoding UTF8
    $result = $resultJson | ConvertFrom-Json

    Write-Verbose "Command execution completed in $($result.executionTimeMs)ms"

    return $result
}

<#
.SYNOPSIS
    Get the most recent command result from the results directory.

.PARAMETER ResultsDir
    Override the default results directory.

.EXAMPLE
    $lastResult = Get-LastRevitCommandResult
    Write-Host "Last command: $($lastResult.id) - Success: $($lastResult.success)"
#>
function Get-LastRevitCommandResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string]$ResultsDir = $script:DefaultResultsDir
    )

    if (-not (Test-Path $ResultsDir)) {
        throw "Results directory not found: $ResultsDir"
    }

    $latestFile = Get-ChildItem -Path $ResultsDir -Filter "results-*.json" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $latestFile) {
        throw "No result files found in: $ResultsDir"
    }

    $resultJson = Get-Content -Path $latestFile.FullName -Raw -Encoding UTF8
    return $resultJson | ConvertFrom-Json
}

<#
.SYNOPSIS
    Clean up old result files to prevent disk bloat.

.PARAMETER ResultsDir
    Override the default results directory.

.PARAMETER OlderThanDays
    Delete files older than this many days (default: 7).

.EXAMPLE
    Clear-OldRevitCommandResults -OlderThanDays 3
#>
function Clear-OldRevitCommandResults {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$false)]
        [string]$ResultsDir = $script:DefaultResultsDir,

        [Parameter(Mandatory=$false)]
        [int]$OlderThanDays = 7
    )

    if (-not (Test-Path $ResultsDir)) {
        Write-Verbose "Results directory not found: $ResultsDir"
        return
    }

    $cutoffDate = (Get-Date).AddDays(-$OlderThanDays)

    Get-ChildItem -Path $ResultsDir -Filter "results-*.json" |
        Where-Object { $_.LastWriteTime -lt $cutoffDate } |
        ForEach-Object {
            Write-Verbose "Deleting old result: $($_.Name)"
            Remove-Item $_.FullName -Force
        }

    Get-ChildItem -Path $ResultsDir -Filter "error-*.txt" |
        Where-Object { $_.LastWriteTime -lt $cutoffDate } |
        ForEach-Object {
            Write-Verbose "Deleting old error: $($_.Name)"
            Remove-Item $_.FullName -Force
        }
}

# Export functions
Export-ModuleMember -Function Invoke-RevitCommand, Get-LastRevitCommandResult, Clear-OldRevitCommandResults
