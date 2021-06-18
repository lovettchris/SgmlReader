
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

pwsh -f Common/setup.ps1

REM Restore NuGet packages:
msbuild -t:restore SgmlReader.sln

REM Build the solution:
msbuild SgmlReader.sln
