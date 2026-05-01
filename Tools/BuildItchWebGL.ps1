param(
    [string]$UnityExe = $env:UNITY_EXE,
    [string]$BuildPath = "Build/ItchWebGL",
    [string]$ZipPath = "Builds/itch/last-kernel-webgl-test.zip",
    [switch]$NoZip
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$VersionFile = Join-Path $ProjectRoot "ProjectSettings/ProjectVersion.txt"
$LogPath = Join-Path $ProjectRoot "Logs/itch-webgl-build.log"

function Get-UnityVersion {
    $versionLine = Get-Content $VersionFile | Where-Object { $_ -match "^m_EditorVersion:" } | Select-Object -First 1
    if (-not $versionLine) {
        throw "Could not read Unity editor version from $VersionFile"
    }

    return ($versionLine -replace "^m_EditorVersion:\s*", "").Trim()
}

function Find-Unity {
    param([string]$Version)

    if ($UnityExe -and (Test-Path $UnityExe)) {
        return (Resolve-Path $UnityExe).Path
    }

    $candidates = @(
        "C:/Program Files/Unity/Hub/Editor/$Version/Editor/Unity.exe",
        "C:/Program Files/Unity/Editor/Unity.exe",
        "E:/Unity/$Version/Editor/Unity.exe",
        "D:/Unity/$Version/Editor/Unity.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Unity.exe was not found. Set UNITY_EXE or pass -UnityExe. Expected project version: $Version"
}

$UnityVersion = Get-UnityVersion
$Unity = Find-Unity -Version $UnityVersion
$ResolvedBuildPath = Join-Path $ProjectRoot $BuildPath
$ResolvedZipPath = Join-Path $ProjectRoot $ZipPath

New-Item -ItemType Directory -Force -Path (Split-Path $LogPath) | Out-Null

Write-Host "Building Last Kernel WebGL test build with Unity $UnityVersion"
Write-Host "Unity: $Unity"
Write-Host "Output: $ResolvedBuildPath"

& $Unity `
    -batchmode `
    -quit `
    -projectPath $ProjectRoot `
    -executeMethod Markyu.LastKernel.ItchWebGLBuild.Build `
    -buildPath $ResolvedBuildPath `
    -logFile $LogPath

if ($LASTEXITCODE -ne 0) {
    throw "Unity build failed with exit code $LASTEXITCODE. See $LogPath"
}

if (-not $NoZip) {
    New-Item -ItemType Directory -Force -Path (Split-Path $ResolvedZipPath) | Out-Null
    if (Test-Path $ResolvedZipPath) {
        Remove-Item $ResolvedZipPath
    }

    Compress-Archive -Path (Join-Path $ResolvedBuildPath "*") -DestinationPath $ResolvedZipPath
    Write-Host "Created $ResolvedZipPath"
}
