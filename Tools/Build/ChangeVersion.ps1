param([string]$version, [string]$prerelease)

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

function RegexReplace ($fileSearch, $replacePattern, $replaceWith)
{
    Get-ChildItem $fileSearch -r `
        | Where-Object { $_.FullName -notlike '*\PackagesCache\*' } `
        | ForEach-Object {
            $c = [IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::Default) -Replace $replacePattern, $replaceWith;
            [IO.File]::WriteAllText($_.FullName, $c, [System.Text.Encoding]::UTF8)
            }
}

Write-Output "Setting version '$fullVersion'."

RegexReplace '*AssemblyInfo.cs' '([\n^]\[assembly: Assembly(File)?Version(Attribute)?\(\").*(\"\)\])' ('${1}'+$version+'${4}')
RegexReplace '*AssemblyInfo.cs' '([\n^]\[assembly: AssemblyInformationalVersion(Attribute)?\(\").*(\"\)\])' ('${1}'+$fullVersion+'${3}')
RegexReplace '*.nuspec' '([\n^]\s*\<version\>).*(\<\/version\>\s*)' ('${1}'+$fullVersion+'${2}')
