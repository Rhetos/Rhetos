param([version]$version, [string]$prerelease)

$ErrorActionPreference = 'Stop'

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

If ($prerelease -eq 'auto')
{
    $prereleaseSuffix = ('-dev' + (get-date -format 'yyMMddHHmm') + (git rev-parse --short HEAD)).Substring(0,20)
}
ElseIf ($prerelease)
{
    $prereleaseSuffix = '-' + $prerelease
}
Else
{
    $prereleaseSuffix = ''
}
$fullVersion = $version.ToString() + $prereleaseSuffix
Write-Output "Setting version '$fullVersion'."

RegexReplace '*AssemblyInfo.cs' '([\n^]\[assembly: Assembly(File)?Version(Attribute)?\(\").*(\"\)\])' ('${1}' + $version + '${4}')
RegexReplace '*AssemblyInfo.cs' '([\n^]\[assembly: AssemblyInformationalVersion(Attribute)?\(\").*(\"\)\])' ('${1}' + $fullVersion + '${3}')
RegexReplace '*.nuspec' '([\n^]\s*\<version\>).*(\<\/version\>\s*)' ('${1}' + $fullVersion + '${2}')
RegexReplace 'Directory.Build.props' '([\n^]\s*\<InformationalVersion\>).*(\<\/InformationalVersion\>\s*)' ('${1}' + $fullVersion + '${2}')
RegexReplace 'Directory.Build.props' '([\n^]\s*\<AssemblyVersion\>).*(\<\/AssemblyVersion\>\s*)' ('${1}' + $version + '${2}')
RegexReplace 'Directory.Build.props' '([\n^]\s*\<FileVersion\>).*(\<\/FileVersion\>\s*)' ('${1}' + $version + '${2}')

# CommonConcepts test projects have generic references with a star suffix ("-dev*") to support any version currently being built (a prerelease with automatic versioning or a final release).
RegexReplace 'CommonConcepts.Test.csproj' '([\n^]\s*\<PackageReference Include=\"Rhetos.*?\" Version=\").*?(\")' ('${1}' + $version + '-dev*${2}')
RegexReplace 'CommonConcepts.TestApp.csproj' '([\n^]\s*\<PackageReference Include=\"Rhetos.*?\" Version=\").*?(\")' ('${1}' + $version + '-dev*${2}')

# CommonConcepts is developed together with Rhetos framework, so it is expected to match the release version. Difference in patch version (Build) is allowed here.
If ($Version.Build -gt 0)
{
    $minFrameworkVersion = $version.Major.ToString() + '.' + $version.Minor.ToString() + '.0'
}
Else
{
    $minFrameworkVersion = $fullVersion # Prerelease must be included here, because its value is less then release version.
}

[string]$nextFrameworkVersion=$version.Major.ToString() + '.' + ($version.Minor + 1).ToString() + '.0'

RegexReplace '*.nuspec' '(dependency id=\"Rhetos.*\" version=\").*?(\")' ('${1}[' + $minFrameworkVersion + ',' + $nextFrameworkVersion + ')${2}')
