$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
Set-Location $RootDir

$props =  Join-Path -Path $RootDir -ChildPath "Common/version.props"
$nuspec = Join-Path -Path $RootDir -ChildPath "SgmlReader.nuspec"
$info = Join-Path -Path $RootDir -ChildPath "SgmlReaderUniversal/Properties/AssemblyInfo.cs"

function UpdateVersion($line)
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

function FixFile($filename)
{
  if ($filename -eq "") {
      Write-Error "Missing filename parameter"
      Exit 1
  }

  $nl = [Environment]::NewLine
  $versionFile = Join-Path -Path $ScriptDir -ChildPath "version.txt"
  $version = Get-Content -Path $versionFile | Join-String -Separator $nl 
  $version = $version.Trim()

  $fullPath = $filename
  if (-Not (Test-Path $fullPath)) {
      $x = Get-Location
      $fullPath = Join-Path -Path $x -ChildPath $filename
  }

  $localcontent = Get-Content -Path $fullPath | ForEach { UpdateVersion $_ } | Join-String -Separator $nl
  $encoding = New-Object System.Text.UTF8Encoding $False
  
  $write = $false
  while (-not $write) {
      try {
        Write-Host "updating version in $filename"
        [IO.File]::WriteAllText($filename, $localcontent, $encoding)
        $write = $true
      } catch {
         Write-Host "file locked, trying again in 1 second: $filename" 
         Start-Sleep -s 1
      }
  }
}

FixFile($props)
FixFile($nuspec)
FixFile($info)
