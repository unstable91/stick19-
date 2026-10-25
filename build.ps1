<#
.SYNOPSIS
    Builds the Desktop Sticky Note solution in Release configuration.
#>

$ErrorActionPreference = "Stop"

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Building stick19 for Windows 11" -ForegroundColor Cyan
Write-Host "Developed by unstable19" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan

$msBuildPath = $null

# 1. Check vswhere
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $vsPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    if ($vsPath -and (Test-Path $vsPath)) {
        $msBuildPath = $vsPath
    }
}

# 2. Check native Windows Framework64 MSBuild
if (-not $msBuildPath) {
    $nativeMsBuild64 = "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
    $nativeMsBuild32 = "$env:SystemRoot\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    if (Test-Path $nativeMsBuild64) {
        $msBuildPath = $nativeMsBuild64
    } elseif (Test-Path $nativeMsBuild32) {
        $msBuildPath = $nativeMsBuild32
    }
}

if (-not $msBuildPath) {
    Write-Error "MSBuild was not found on this system."
    exit 1
}

Write-Host "Using MSBuild: $msBuildPath" -ForegroundColor Gray
$slnPath = Join-Path $PSScriptRoot "DesktopStickyNote.sln"

& $msBuildPath $slnPath /p:Configuration=Release /p:Platform=x64 /p:MSBuildWarningsAsMessages=MSB3644 /v:minimal /nologo

if ($LASTEXITCODE -eq 0) {
    $outExe = Join-Path $PSScriptRoot "bin\Release\stick19.exe"
    Write-Host "`n========================================================" -ForegroundColor Green
    Write-Host "Build SUCCESSFUL!" -ForegroundColor Green
    Write-Host "Binary: $outExe" -ForegroundColor Green
    Write-Host "========================================================" -ForegroundColor Green
} else {
    Write-Error "Build failed with exit code $LASTEXITCODE"
}
