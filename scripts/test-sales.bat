@echo off
setlocal ENABLEDELAYEDEXPANSION

set DOTNET_NOLOGO=1

rem Determine repo root = parent of this script directory
set SCRIPT_DIR=%~dp0
pushd "%SCRIPT_DIR%\.."

if "%~1"=="" goto :help
set CMD=%~1

rem Ensure dotnet
dotnet --version >nul 2>&1
if errorlevel 1 (
  echo [ERROR] dotnet SDK not found. Install .NET SDK 9.0 or later.
  popd
  exit /b 1
)

rem Build solution in Release
dotnet build ".\DeveloperStore.sln" -c Release
if errorlevel 1 (
  echo [ERROR] Build failed.
  popd
  exit /b 1
)

if /I "%CMD%"=="all" goto :all
if /I "%CMD%"=="unit" goto :unit
if /I "%CMD%"=="functional" goto :functional
if /I "%CMD%"=="integration" goto :integration
if /I "%CMD%"=="no-integration" goto :nointegration
if /I "%CMD%"=="coverage" goto :coverage
if /I "%CMD%"=="help" goto :help

echo [ERROR] Unknown command: %CMD%
goto :help

:check_docker
docker info >nul 2>&1
if errorlevel 1 (
  echo [WARN] Docker is not available or not running. Integration tests cannot be executed.
  set DOCKER_AVAILABLE=0
) else (
  set DOCKER_AVAILABLE=1
)
goto :eof

:all
call :check_docker
if "!DOCKER_AVAILABLE!"=="1" (
  dotnet test ".\DeveloperStore.sln" -c Release --logger "trx;LogFileName=TestResults.trx" --results-directory ".\TestResults"
) else (
  echo [INFO] Running all tests except Integration - Docker unavailable
  dotnet test ".\DeveloperStore.sln" -c Release --filter "FullyQualifiedName!~Sales.IntegrationTests" --logger "trx;LogFileName=TestResults.trx" --results-directory ".\TestResults"
)
goto :end

:unit
dotnet test ".\DeveloperStore.sln" -c Release --filter "FullyQualifiedName~Sales.UnitTests" --logger "trx;LogFileName=TestResults.trx" --results-directory ".\TestResults"
goto :end

:functional
dotnet test ".\DeveloperStore.sln" -c Release --filter "FullyQualifiedName~Sales.FunctionalTests" --logger "trx;LogFileName=TestResults.trx" --results-directory ".\TestResults"
goto :end

:integration
call :check_docker
if "!DOCKER_AVAILABLE!"=="0" (
  echo [ERROR] Docker is required for Integration tests. Start Docker Desktop and retry.
  popd
  exit /b 2
)
dotnet test ".\DeveloperStore.sln" -c Release --filter "FullyQualifiedName~Sales.IntegrationTests" --logger "trx;LogFileName=TestResults.trx" --results-directory ".\TestResults"
goto :end

:nointegration
dotnet test ".\DeveloperStore.sln" -c Release --filter "FullyQualifiedName!~Sales.IntegrationTests" --logger "trx;LogFileName=TestResults.trx" --results-directory ".\TestResults"
goto :end

:coverage
call :check_docker
if "!DOCKER_AVAILABLE!"=="1" (
  dotnet test ".\DeveloperStore.sln" -c Release --collect "XPlat Code Coverage" --results-directory ".\TestResults"
) else (
  echo [INFO] Docker unavailable: collecting coverage excluding Integration tests...
  dotnet test ".\DeveloperStore.sln" -c Release --filter "FullyQualifiedName!~Sales.IntegrationTests" --collect "XPlat Code Coverage" --results-directory ".\TestResults"
)
goto :end

:help
echo Usage: scripts\test-sales.bat ^<command^>
echo.
echo Commands:
echo   all             Run all test projects (requires Docker for Integration)
echo   unit            Run unit tests only (Sales.UnitTests)
echo   functional      Run functional API tests only (Sales.FunctionalTests)
echo   integration     Run integration tests only (requires Docker)
echo   no-integration  Run all tests except Integration
echo   coverage        Run tests with code coverage collection
echo   help            Show this help
echo.
echo Examples:
echo   scripts\test-sales.bat all
echo   scripts\test-sales.bat unit
echo   scripts\test-sales.bat no-integration
goto :end

:end
set EXITCODE=%ERRORLEVEL%
popd
endlocal & exit /b %EXITCODE%