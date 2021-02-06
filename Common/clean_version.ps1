param ([string] $filename)

if ($filename -eq "") {
    Write-Error "Missing filename parameter"
    Exit 1
}

$x = Get-Location
$fullPath = Join-Path -Path $x -ChildPath $filename

if (-Not (Test-Path -Path $fullPath)) {
    # file was deleted
    Exit 1
}

function CleanVersion($line)
{
    if ($line -match "\w*\<version\>([^\<]*)\</version\>") {
        # this is the SgmlReader.nuspec
        return $line.Replace($Matches.1, "`$version")
    }
    elseif ($line -match "\w*\<Version\>([^\<]*)\</Version\>") {
        # this is in version.props.
        return $line.Replace($Matches.1, "`$version")
    }
    elseif ($line -match "\w*\<FileVersion\>([^\<]*)\</FileVersion\>") {
        # this is in version.props.
        return $line.Replace($Matches.1, "`$version")
    }
    elseif ($line -match "\w*\<AssemblyVersion\>([^\<]*)\</AssemblyVersion\>") {
        # this is in version.props.
        return $line.Replace($Matches.1, "`$version")
    }
    elseif ($line -match "\w*\<AssemblyInformationalVersion\>([^\<]*)\</AssemblyInformationalVersion\>") {
        # this is in version.props.
        return $line.Replace($Matches.1, "`$version")
    }
    elseif ($line -match "\w*\[assembly\: AssemblyVersion\(\`"([^<]*)\`"\)\]") {
        return $line.Replace($Matches.1, "`$version")
    }
    elseif ($line -match "\w*\[assembly\: AssemblyFileVersion\(\`"([^<]*)\`"\)\]") {
        return $line.Replace($Matches.1, "`$version")
    }
    elseif ($line -match "\w*\[assembly\: AssemblyInformationalVersion\(\`"([^<]*)\`"\)\]") {
        return $line.Replace($Matches.1, "`$githash")
    }
    return $line
}

# git requires \n newlines.
$nl = "`n"
$gitcontent = Get-Content -Path $fullPath | ForEach { CleanVersion $_ } | Join-String -Separator $nl

Write-Host $gitcontent