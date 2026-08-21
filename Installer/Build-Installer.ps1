$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot "TurnSteamOn\TurnSteamOn.csproj"
$publishDirectory = Join-Path $repositoryRoot "TurnSteamOn\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
$script = Join-Path $PSScriptRoot "TurnSteamOn.iss"
$iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue

dotnet publish $project -c Release -r win-x64 --self-contained true

if ($null -eq $iscc) {
    throw "Inno Setup compiler was not found. Install Inno Setup and ensure iscc.exe is on PATH. Published files are available at $publishDirectory."
}

& $iscc.Source $script