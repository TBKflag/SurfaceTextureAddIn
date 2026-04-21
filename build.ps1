param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [string]$Framework = "net48",
    [string]$SolidWorksInteropDir = "D:\Program Files\SOLIDWORKS Corp\SOLIDWORKS",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptDir "src\SurfaceTextureAddIn\SurfaceTextureAddIn.csproj"
$interopRedistPath = Join-Path $SolidWorksInteropDir "api\redist"
$interopAssemblies = @(
    "SolidWorks.Interop.sldworks.dll",
    "SolidWorks.Interop.swconst.dll",
    "SolidWorks.Interop.swpublished.dll"
)

if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

if (-not (Test-Path $interopRedistPath)) {
    throw "SolidWorks interop redist directory not found: $interopRedistPath`nUse -SolidWorksInteropDir to point to your SolidWorks 2020 installation root."
}

foreach ($interopFile in $interopAssemblies) {
    $interopPath = Join-Path $interopRedistPath $interopFile
    if (-not (Test-Path $interopPath)) {
        throw "Required interop file not found: $interopPath"
    }
}

$buildArgs = @(
    "build", $projectPath,
    "-c", $Configuration,
    "-p:SolidWorksInteropDir=$SolidWorksInteropDir"
)

if (-not [string]::IsNullOrWhiteSpace($OutputRoot)) {
    $resolvedOutputRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir $OutputRoot))
    $outDir = Join-Path $resolvedOutputRoot "$Configuration\$Framework\"
    $buildArgs += "-p:OutDir=$outDir"
}

Write-Host "Building project: $projectPath"
Write-Host "Configuration: $Configuration"
Write-Host "Framework: $Framework"
Write-Host "SolidWorks interop: $interopRedistPath"

& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "Build failed. Exit code: $LASTEXITCODE"
}

if (-not [string]::IsNullOrWhiteSpace($OutputRoot)) {
    $targetDir = $outDir
} else {
    $targetDir = Join-Path $scriptDir "src\SurfaceTextureAddIn\bin\$Configuration\$Framework"
}

if (-not (Test-Path $targetDir)) {
    throw "Build output directory not found: $targetDir"
}

foreach ($interopFile in $interopAssemblies) {
    $sourcePath = Join-Path $interopRedistPath $interopFile
    $targetPath = Join-Path $targetDir $interopFile
    Copy-Item $sourcePath $targetPath -Force
    Write-Host "Copied interop dependency: $targetPath"
}

Write-Host ""
Write-Host "Build complete."
Write-Host "Output: $targetDir"
