# This script checks for and installs Azurite if it's not already present,
# then starts the Azurite storage emulator.

# Check if npm is installed
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "npm is not installed. Trying to install Node.js using Winget..."
    # Check if Winget is installed
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        winget install OpenJS.NodeJS
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to install Node.js using Winget. Please install it manually." -ForegroundColor Red
            exit 1
        }
        Write-Host "Node.js installed. Please restart your PowerShell session and run this script again." -ForegroundColor Yellow
        exit 0
    } else {
        Write-Host "Winget not found. Please install Node.js and npm manually to use this script." -ForegroundColor Red
        exit 1
    }
}

# Check if Azurite is installed
if (-not (Get-Command azurite -ErrorAction SilentlyContinue)) {
    Write-Host "Azurite is not installed. Installing via npm..."
    npm install -g azurite
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install Azurite. Please install it manually using 'npm install -g azurite'." -ForegroundColor Red
        exit 1
    }
}

# Define a path for Azurite's data within the solution directory
$dataDirectory = Join-Path $PSScriptRoot "AzuriteData"

# Create the directory if it doesn't exist
if (-not (Test-Path $dataDirectory)) {
    Write-Host "Creating directory for Azurite data at '$dataDirectory'..."
    New-Item -ItemType Directory -Path $dataDirectory | Out-Null
}

# Start Azurite
Write-Host "Starting Azurite..."
Write-Host "Data will be stored in: $dataDirectory"
azurite --location $dataDirectory
