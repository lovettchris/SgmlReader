@echo off

@WHERE msbuild
@IF %ERRORLEVEL% NEQ 0 (
	@echo "Run this script from the Visual Studio Developer Command Prompt"
	exit /B
)

@WHERE pwsh >NUL 2>NUL
@IF %ERRORLEVEL% NEQ 0 (
	@echo "Installing 'pwsh' (PowerShell Core)"
	dotnet tool install --global powershell
)

pwsh -f Common/fix_versions.ps1

REM Restore NuGet packages:
msbuild -t:restore SgmlReader.sln /verbosity:minimal

REM Build the solution:
msbuild SgmlReader.sln /verbosity:minimal

REM run .net core tests
vstest.console d:\git\lovettchris\SgmlReader\SgmlTests\bin\Release\net5.0\SgmlTests.dll

if "%MyKeyFile%" == "" goto :eof

REM sign assemblies
pwsh -f Common\full_sign.ps1

REM create .nupkg
nuget pack SgmlReader.nuspec

