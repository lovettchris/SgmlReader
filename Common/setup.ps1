$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$fix = Join-Path -Path $ScriptDir -ChildPath "fix_versions.ps1"

$x = git config --get filter.version.smudge
if ($x -eq $null) {
    &git config --add filter.version.smudge "pwsh -f Common/smudge_version.ps1 %f"
    &git config --add filter.version.clean "pwsh -f Common/clean_version.ps1 %f"
}

&$fix
