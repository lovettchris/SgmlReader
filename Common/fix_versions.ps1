$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

$smudge = Join-Path -Path $ScriptDir -ChildPath "smudge_version.ps1"

$props = Join-Path -Path $ScriptDir -ChildPath "version.props"
$nuspec = Join-Path -Path $RootDir -ChildPath "SgmlReader.nuspec"
$info = Join-Path -Path $RootDir -ChildPath "SgmlReaderUniversal/Properties/AssemblyInfo.cs"

function FixFile($filename){
    $content = &$smudge $filename
    Write-Host $content
}

Write-Host $props
FixFile($props)