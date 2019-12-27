param([string]$version, [string]$prerelease)

$ErrorActionPreference = 'Stop'

If ($prerelease -eq 'auto')
{
    $prerelease = ('dev'+(get-date -format 'yyMMddHHmm')+(git rev-parse --short HEAD)).Substring(0,20)
}

If ($prerelease)
{
    $fullVersion = $version + '-' + $prerelease
}
Else
{
    $fullVersion = $version
}

[string[]]$exclude = @('bin', 'obj', 'packages', 'TestResults', 'PackagesCache', 'Install', '.*', 'Logs')
[string[]]$folders = @()
[string[]]$foldersNew = Get-Location
while ($foldersNew.Count -gt 0)
{
    [string[]]$folders += $foldersNew
    [string[]]$foldersNew = Get-ChildItem $foldersNew -Directory -Exclude $exclude
}
$folders = $folders | Sort-Object

function RegexReplace ($fileSearch, $replacePattern, $replaceWith)
{
    Get-ChildItem -File -Path $folders -Filter $fileSearch `
        | Select-Object -ExpandProperty FullName `
        | ForEach-Object {
            $text = [IO.File]::ReadAllText($_, [System.Text.Encoding]::UTF8)
            $replaced = $text -Replace $replacePattern, $replaceWith
            if ($replaced -ne $text) {
                Write-Output $_
                [IO.File]::WriteAllText($_, $replaced, [System.Text.Encoding]::UTF8)
            }
        }
}

Write-Output "Setting version '$fullVersion'."

RegexReplace '*AssemblyInfo.cs' '([\n^]\[assembly: Assembly(File)?Version(Attribute)?\(\").*(\"\)\])' ('${1}'+$version+'${4}')
RegexReplace '*AssemblyInfo.cs' '([\n^]\[assembly: AssemblyInformationalVersion(Attribute)?\(\").*(\"\)\])' ('${1}'+$fullVersion+'${3}')
RegexReplace '*.nuspec' '([\n^]\s*\<version\>).*(\<\/version\>\s*)' ('${1}'+$fullVersion+'${2}')
