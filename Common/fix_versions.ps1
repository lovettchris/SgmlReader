$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
Set-Location $RootDir

$smudge = Join-Path -Path $ScriptDir -ChildPath "smudge_version.ps1"

$props =  Join-Path -Path $RootDir -ChildPath "Common/version.props"
$info = Join-Path -Path $RootDir -ChildPath "SgmlReaderUniversal/Properties/AssemblyInfo.cs"

function FixFile($filename)
{
    $content = &$smudge $filename $True
}

FixFile($props)
FixFile($nuspec)
FixFile($info)
