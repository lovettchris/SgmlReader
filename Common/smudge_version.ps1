param ([string] $filename)

if ($filename -eq "") {
    Write-Error "Missing filename parameter"
    Exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path    
$x = Get-Location
$fullPath = Join-Path -Path $x -ChildPath $filename

if (-Not (Test-Path -Path $fullPath)) {
    # file was deleted
    Exit 1
}

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

function SmudgeFile($fullPath)
{
    $nl = [Environment]::NewLine
    $githash = &git rev-parse HEAD

    $versionFile = Join-Path -Path $ScriptDir -ChildPath "version.txt"
    $version = Get-Content -Path $versionFile | Join-String -Separator $nl 
    $version = $version.Trim()

    $content = Get-Content -Path $fullPath | ForEach { SmudgeVersion $_ }

    return Join-String -Separator $nl -InputObject  $content
}

$localcontent = SmudgeFile($fullPath)
Write-Host $localcontent