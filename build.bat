@echo off
REM CoreApiClient Build Script for Windows
REM This script builds, tests, and packages the CoreApiClient library

setlocal enabledelayedexpansion

echo =========================================
echo CoreApiClient Build Script
echo =========================================
echo.

REM Check if .NET is installed
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK is not installed
    echo Please install .NET 8.0 SDK or later from https://dotnet.microsoft.com/download
    exit /b 1
)

REM Display .NET version
echo [OK] .NET SDK found
dotnet --version
echo.

REM Step 1: Clean
echo Step 1: Cleaning previous builds...
dotnet clean -c Release >nul 2>&1
if exist "src\CoreApiClient\bin\Release\*.nupkg" del /q "src\CoreApiClient\bin\Release\*.nupkg" >nul 2>&1
if exist "artifacts" rmdir /s /q artifacts >nul 2>&1
echo [OK] Clean completed
echo.

REM Step 2: Restore
echo Step 2: Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo [ERROR] Restore failed
    exit /b 1
)
echo [OK] Restore completed
echo.

REM Step 3: Build
echo Step 3: Building solution...
dotnet build -c Release --no-restore
if %errorlevel% neq 0 (
    echo [ERROR] Build failed
    exit /b 1
)
echo [OK] Build completed
echo.

REM Step 4: Run Tests
echo Step 4: Running tests...
dotnet test --no-build -c Release --verbosity quiet
if %errorlevel% neq 0 (
    echo [ERROR] Tests failed
    exit /b 1
)
echo [OK] All tests passed
echo.

REM Step 5: Pack
echo Step 5: Creating NuGet package...
dotnet pack src\CoreApiClient\CoreApiClient.csproj -c Release --no-build -o .\artifacts
if %errorlevel% neq 0 (
    echo [ERROR] Packaging failed
    exit /b 1
)
echo [OK] Package created
echo.

REM Step 6: Display package info
echo Step 6: Package information...
for %%f in (artifacts\CoreApiClient.*.nupkg) do (
    if not "%%~nxf"=="*symbols*" (
        echo [OK] Package location: %%f
        dir %%f | find "%%~nxf"
    )
)
echo.

REM Step 7: Run examples (if requested)
if "%1"=="--run-examples" (
    echo Step 7: Running examples...
    cd examples\CoreApiClient.Examples
    dotnet run --no-build -c Release
    cd ..\..
    echo.
)

echo =========================================
echo [OK] Build completed successfully!
echo =========================================
echo.
echo Next steps:
echo   1. Review the package in the artifacts folder
echo   2. Test locally: dotnet add package [path-to-nupkg]
echo   3. Publish to NuGet: dotnet nuget push artifacts\CoreApiClient.*.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
echo.

endlocal
