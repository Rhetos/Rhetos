$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Path "Install" -Force > $null
Remove-Item -Path 'Install\*' -Force -Recurse

$packages = Get-ChildItem Source\*.nuspec, CommonConcepts\*.nuspec | ForEach-Object { $_.FullName.Substring((Get-Location).Path.Length + 1) }

# Running nuget pack jobs in parallel to save 10 seconds in total execution time.
$jobs = $packages | ForEach-Object {
    Start-Job -ScriptBlock {
        param($package)
        Set-Location $using:PWD
        NuGet.exe pack $package -OutputDirectory Install
    } -ArgumentList $_
}

'Running nuget pack jobs.'
$outputs = $jobs | ForEach-Object { $_ | Wait-Job | Receive-Job }
$outputs
$jobs | Remove-Job
