param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$Framework = "net48",
    [string]$SolidWorksInteropDir = "D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS"
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)

if (-not $isAdmin) {
    throw "Please run this script as Administrator."
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dllPath = Join-Path $scriptDir "src\SurfaceTextureAddIn\bin\$Configuration\$Framework\SurfaceTextureAddIn.dll"
$regAsmPath = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
$interopRedistPath = Join-Path $SolidWorksInteropDir "api\redist"
$interopAssemblies = @(
    "SolidWorks.Interop.sldworks.dll",
    "SolidWorks.Interop.swconst.dll",
    "SolidWorks.Interop.swpublished.dll"
)

if (-not (Test-Path $regAsmPath)) {
    throw "RegAsm not found: $regAsmPath"
}

if (-not (Test-Path $dllPath)) {
    throw "Plugin DLL not found: $dllPath`nBuild the project first in Visual Studio."
}

if (-not (Test-Path $interopRedistPath)) {
    throw "SolidWorks interop redist directory not found: $interopRedistPath`nUse -SolidWorksInteropDir to point to your SolidWorks 2020 installation root."
}

foreach ($interopFile in $interopAssemblies) {
    $interopPath = Join-Path $interopRedistPath $interopFile
    if (-not (Test-Path $interopPath)) {
        throw "Required interop file not found: $interopPath`nUse -SolidWorksInteropDir to point to your SolidWorks 2020 installation root."
    }

    $pluginDir = Split-Path -Parent $dllPath
    $pluginInteropPath = Join-Path $pluginDir $interopFile
    if (-not (Test-Path $pluginInteropPath)) {
        Copy-Item $interopPath $pluginInteropPath -Force
        Write-Host "Copied interop dependency: $pluginInteropPath"
    }
}

Write-Host "Using RegAsm: $regAsmPath"
Write-Host "Registering: $dllPath"
Write-Host "SolidWorks interop: $interopRedistPath"

& $regAsmPath $dllPath /codebase

if ($LASTEXITCODE -ne 0) {
    throw "Registration failed. RegAsm exit code: $LASTEXITCODE"
}

Write-Host ""
Write-Host "Registration complete. Open SolidWorks -> Tools -> Add-Ins and enable 'Surface Texture Add-In'."
