# start-bge-m3.ps1
# Starts a local OpenAI-compatible embedding server using either LocalAI or OLama with a bge-m3 model mounted.
# Requirements: Docker installed and running (if using -UseDocker). Put your model file into ./models/bge-m3/

param(
  [ValidateSet('localai','olama')]
  [string]$Server = 'localai',
  [string]$Image = 'ghcr.io/go-skynet/local-ai:latest',
  [string]$ModelHostDir = "$(Join-Path $PSScriptRoot 'models\bge-m3')",
  [string]$ModelFileName = '',        # e.g. 'bge-m3.gguf' - leave empty to use model repository directory
  [string]$ContainerName = 'llm-server',
  [int]$Port = 8080,
  [switch]$UseDocker
)

# Ensure Docker is running if user requested to use docker
if ($UseDocker) {
  try {
    docker info > $null 2>&1
  } catch {
    Write-Error "Docker does not appear to be running or not in PATH. Start Docker Desktop and retry."
    exit 1
  }
}

# Ensure model host directory exists
if (-not (Test-Path $ModelHostDir)) {
  New-Item -ItemType Directory -Path $ModelHostDir -Force | Out-Null
  Write-Host "Created model directory: $ModelHostDir"
  Write-Host "Place your bge-m3 model file (ggml/gguf/gguf2) into that directory and re-run the script."
  exit 0
}

# Resolve full host path and build mount string safely (delimit variable so ':' is not parsed as part of the variable name)
try {
  $hostDir = (Resolve-Path $ModelHostDir).ProviderPath
} catch {
  Write-Error "Failed to resolve model host directory: $_"
  exit 1
}

$hostMount = "${hostDir}:/models"

# Pull image if using Docker
if ($UseDocker) {
  Write-Host "Pulling image $Image ..."
  docker pull $Image

  # Stop & remove any existing container
  $existing = docker ps -a --format '{{.Names}}' | Where-Object { $_ -eq $ContainerName }
  if ($existing) {
    Write-Host "Removing existing container $ContainerName ..."
    docker rm -f $ContainerName | Out-Null
  }
}

# Determine server command based on selected server implementation
if ($Server -eq 'localai') {
  if ([string]::IsNullOrWhiteSpace($ModelFileName)) {
    $serverCmd = "local-ai --model-repository /models --listen 0.0.0.0:$Port"
  } else {
    $serverCmd = "local-ai --model bge-m3=/models/$ModelFileName --listen 0.0.0.0:$Port"
  }
} else {
  # OLama-specific command templates.
  # Note: OLama CLI flags differ between versions; adjust these flags if your OLama build uses different options.
  if ([string]::IsNullOrWhiteSpace($ModelFileName)) {
    # tell OLama to use the model repository directory
    $serverCmd = "olama serve --model-repository /models --listen 0.0.0.0:$Port"
  } else {
    # map single model file (alias 'bge-m3')
    $serverCmd = "olama serve --model bge-m3=/models/$ModelFileName --listen 0.0.0.0:$Port"
  }
}

if ($UseDocker) {
  # Build docker run args
  $runArgs = @(
    "--rm",
    "--name", $ContainerName,
    "-p", "${Port}:8080",
    "-v", $hostMount
  )

  Write-Host "Starting container $ContainerName (port $Port) using image $Image ..."
  # Run the container. Use the call operator to expand the run args array.
  & docker run @runArgs $Image $serverCmd
} else {
  # Run server locally (requires LocalAI/OLama binary installed and on PATH)
  Write-Host "Starting $Server server locally using command: $serverCmd"
  # Use Start-Process so the server runs and outputs to its own window; remove -NoNewWindow if you prefer inline
  Start-Process -FilePath 'powershell' -ArgumentList "-NoExit","-Command","$serverCmd"
}

Write-Host "Server start requested. Embeddings endpoint: http://localhost:$Port/v1/embeddings"
Write-Host "Test with curl (example):"
Write-Host "curl -X POST http://localhost:$Port/v1/embeddings -H 'Content-Type: application/json' -d '{""input"": [""test text""] , ""model"": ""bge-m3"" }'"
