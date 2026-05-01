param(
    [Parameter(Mandatory = $true)]
    [string]$ItchTarget,
    [string]$BuildPath = "Build/ItchWebGL",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ResolvedBuildPath = Join-Path $ProjectRoot $BuildPath

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot "BuildItchWebGL.ps1") -BuildPath $BuildPath -NoZip
}

if (-not (Get-Command butler -ErrorAction SilentlyContinue)) {
    throw "butler is not on PATH. Install it from itch.io or add it to PATH, then run this again."
}

if (-not (Test-Path (Join-Path $ResolvedBuildPath "index.html"))) {
    throw "No WebGL build found at $ResolvedBuildPath. Run Tools/BuildItchWebGL.ps1 first."
}

butler push $ResolvedBuildPath $ItchTarget
