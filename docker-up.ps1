$ErrorActionPreference = 'Stop'

$docker = Get-Command docker -ErrorAction SilentlyContinue
if ($docker) {
    $dockerPath = $docker.Source
} else {
    $dockerPath = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe'
}

if (-not (Test-Path $dockerPath)) {
    throw 'Docker не найден. Установите Docker Desktop и перезапустите PowerShell.'
}

& $dockerPath compose up --build
