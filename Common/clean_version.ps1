param ([string] $filename)

if ($filename -eq "") {
    Write-Error "Missing filename parameter"
    Exit 1
}

function CleanVersion($line) {
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

$x = Get-Location
$fullPath = Join-Path -Path $x -ChildPath $filename

$content = Get-Content -Path $fullPath | ForEach { CleanVersion $_ }

# git requires unix style newslines in the cleaned file.
$nl = "`n"
$gitcontent = Join-String -Separator $nl -InputObject $content

Write-Host $gitcontent