$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot "TurnSteamOn\TurnSteamOn.csproj"
$publishDirectory = Join-Path $repositoryRoot "TurnSteamOn\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
$script = Join-Path $PSScriptRoot "TurnSteamOn.iss"
$isccCommand = Get-Command iscc.exe -ErrorAction SilentlyContinue
$isccPath = if ($null -ne $isccCommand) { $isccCommand.Source } else { $null }

if ([string]::IsNullOrWhiteSpace($isccPath)) {
    $possiblePaths = @(
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    $isccPath = $possiblePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

dotnet publish $project -c Release -r win-x64 --self-contained true

if ([string]::IsNullOrWhiteSpace($isccPath)) {
    throw "Inno Setup compiler was not found. Install Inno Setup and ensure iscc.exe is on PATH. Published files are available at $publishDirectory."
}

& $isccPath $script