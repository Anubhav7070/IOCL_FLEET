# build.ps1 - Bootstrap Web Forms dependencies and compile local hosting utilities

$ErrorActionPreference = "Stop"

# 1. Setup folders
$baseDir = "c:\Users\Lenovo\Downloads\IOCL\web-forms"
$binDir = Join-Path $baseDir "bin"
$uploadsDir = Join-Path $baseDir "uploads"
$appCodeDir = Join-Path $baseDir "App_Code"

Write-Host "Creating directories..."
New-Item -ItemType Directory -Force -Path $binDir
New-Item -ItemType Directory -Force -Path $uploadsDir
New-Item -ItemType Directory -Force -Path $appCodeDir

# 2. Download nuget.exe
$nugetPath = Join-Path $baseDir "nuget.exe"
if (-not (Test-Path $nugetPath)) {
    Write-Host "Downloading nuget.exe..."
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetPath
}

# 3. Install packages via NuGet
$packagesDir = Join-Path $baseDir "packages"
New-Item -ItemType Directory -Force -Path $packagesDir

function Install-Package($id, $version) {
    Write-Host "Installing package $id ($version)..."
    & $nugetPath install $id -Version $version -OutputDirectory $packagesDir -Verbosity quiet
}

# Install SQLite, BCrypt.Net-Next, iTextSharp (PDF) and QRCoder
Install-Package "System.Data.SQLite.Core" "1.0.118"
Install-Package "BCrypt.Net-Next" "4.0.3"
Install-Package "iTextSharp" "5.5.13.3"
Install-Package "QRCoder" "1.4.3"

# 4. Copy DLLs to bin folder
Write-Host "Copying libraries to bin..."
$sqlitePkgDir = "$packagesDir\Stub.System.Data.SQLite.Core.NetFramework.1.0.118.0"
Copy-Item "$sqlitePkgDir\lib\net46\System.Data.SQLite.dll" $binDir -Force

# Native SQLite Interop DLL (requires x64 and x86 folders or directly in bin)
New-Item -ItemType Directory -Force -Path (Join-Path $binDir "x64")
New-Item -ItemType Directory -Force -Path (Join-Path $binDir "x86")
Copy-Item "$sqlitePkgDir\build\net46\x64\SQLite.Interop.dll" (Join-Path $binDir "x64") -Force
Copy-Item "$sqlitePkgDir\build\net46\x86\SQLite.Interop.dll" (Join-Path $binDir "x86") -Force

Copy-Item "$packagesDir\BCrypt.Net-Next.4.0.3\lib\net462\BCrypt.Net-Next.dll" $binDir -Force
Copy-Item "$packagesDir\iTextSharp.5.5.13.3\lib\itextsharp.dll" $binDir -Force
Copy-Item "$packagesDir\QRCoder.1.4.3\lib\net40\QRCoder.dll" $binDir -Force

Write-Host "Dependencies installed successfully in web-forms/bin"
