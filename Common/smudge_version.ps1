param ([string] $filename, [bool] $inplace=$False)

if ($filename -eq "") {
    Write-Error "Missing filename parameter"
    Exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$nl = [Environment]::NewLine
$githash = &git rev-parse HEAD
$versionFile = Join-Path -Path $ScriptDir -ChildPath "version.txt"
$version = Get-Content -Path $versionFile | Join-String -Separator $nl 
$version = $version.Trim()

function SmudgeVersion($line)
{
    if ($line -match "\w*\<version\>([^\<]*)\</version\>") {
        # this is the SgmlReader.nuspec
        return $line.Replace($Matches.1, "$version")
    }
    elseif ($line -match "\w*\<Version\>([^\<]*)\</Version\>") {
        # this is in version.props.
        return $line.Replace($Matches.1, "$version")
    }
    elseif ($line -match "\w*\<FileVersion\>([^\<]*)\</FileVersion\>") {
        # this is in version.props.
        return $line.Replace($Matches.1, "$version")
    }
    elseif ($line -match "\w*\<AssemblyVersion\>([^\<]*)\</AssemblyVersion\>") {
        # this is in version.props.
        return $line.Replace($Matches.1, "$version")
    }
    elseif ($line -match "\w*\[assembly\: AssemblyVersion\(\`"([^<]*)\`"\)\]") {
        return $line.Replace($Matches.1, "$version")
    }
    elseif ($line -match "\w*\[assembly\: AssemblyFileVersion\(\`"([^<]*)\`"\)\]") {
        return $line.Replace($Matches.1, "$version")
    }
    return $line
}

$fullPath = $filename
if (-Not (Test-Path $fullPath)) {
    $x = Get-Location
    $fullPath = Join-Path -Path $x -ChildPath $filename
}

$localcontent = Get-Content -Path $fullPath | ForEach { SmudgeVersion $_ } | Join-String -Separator $nl

if ($inplace) {
    $encoding = New-Object System.Text.UTF8Encoding $False
    [IO.File]::WriteAllText($filename, $localcontent, $encoding)
} else {
    Write-Host $localcontent
}
