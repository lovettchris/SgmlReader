# This script performs the full signing of all the assemblies used in the .nuspec after the build does delay signing
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$testpath = $ScriptDir + "\..\SgmlReader\bin\Release\net46\SgmlReaderDll.dll"
if (-not (Test-Path -Path $testpath)){
	Write-Host "Please build release build of SgmlReader.sln and SgmlReaderUniversal.sln"
	exit 1
}

$doc = [System.Xml.Linq.XDocument]::Load("$ScriptDir\..\SgmlReader.nuspec")
$ns = $doc.Root.Name.Namespace.NamespaceName
foreach ($e in $doc.Root.Descendants()) {
	if ($e.Name.LocalName -eq "file") {
		$src = $e.Attribute("src").Value
		if ($src.EndsWith(".dll") -or $src.EndsWith(".exe")) {
			Write-Host "signing $src"
			$path = $ScriptDir + "\..\" + $src
			signtool sign /a /i "Sectigo Public Code Signing CA R36" /t http://timestamp.sectigo.com /fd sha256 $path
		}
	}
}
