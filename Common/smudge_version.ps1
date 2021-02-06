param ([string] $filename)

if ($filename -eq "") {
    Write-Error "Missing filename parameter"
    Exit 1
}

$nl = [Environment]::NewLine
$githash = &git rev-parse HEAD
$x = Get-Location
$versionFile = Join-Path -Path $x -ChildPath "Common/version.txt"
$version = Get-Content -Path $versionFile | Join-String -Separator $nl 
$version = $version.Trim()

function SmudgeVersion($line) {
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

$fullPath = Join-Path -Path $x -ChildPath $filename

$content = Get-Content -Path $fullPath | ForEach { SmudgeVersion $_ }

$localcontent = Join-String -Separator $nl -InputObject  $content

Write-Host $localcontent