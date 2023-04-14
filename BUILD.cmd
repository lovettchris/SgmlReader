@echo off

WHERE msbuild >NUL 2>NUL
IF ERRORLEVEL 1 goto :setup
goto :powershell

:setup
if "%VS140COMNTOOLS%" NEQ "" (
	call "%VS140COMNTOOLS%vsvars32.bat"
	goto :powershell
)
if "%VS150COMNTOOLS%" NEQ "" (
	call "%VS150COMNTOOLS%VsDevCmd.bat"
	goto :powershell
)
if "%VS160COMNTOOLS%" NEQ "" (
	call "%VS160COMNTOOLS%VsDevCmd.bat"
	goto :powershell
) else (
	echo "Run this script from the Visual Studio Developer Command Prompt"
	exit /B 1
)

:powershell
WHERE pwsh >NUL 2>NUL
IF ERRORLEVEL 1 (
	echo "Installing 'pwsh' (PowerShell Core)"
	dotnet tool install --global powershell
)

pwsh -f Common/fix_versions.ps1

REM Restore NuGet packages:
msbuild -t:restore SgmlReader.sln /verbosity:minimal /p:Configuration=Release
if ERRORLEVEL 1 exit /B 1

REM Build SgmlReader:
msbuild SgmlReader.sln /verbosity:minimal /p:Configuration=Release
if ERRORLEVEL 1 exit /B 1

REM Build SgmlReaderUniversal:
msbuild -t:restore SgmlReaderUniversal.sln /verbosity:minimal /p:Configuration=Release
msbuild SgmlReaderUniversal.sln /verbosity:minimal /p:Configuration=Release
if ERRORLEVEL 1 exit /B 1

REM run .net core tests
vstest.console %~dp0SgmlTests\bin\Release\net7.0\SgmlTests.dll
if ERRORLEVEL 1 exit /B 1

if "%MyKeyFile%" == "" goto :eof

REM sign assemblies
pwsh -f Common\full_sign.ps1

REM create .nupkg
nuget pack SgmlReader.nuspec
