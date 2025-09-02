$ErrorActionPreference = 'Stop'

$root = (Get-Location).Path
$nuget = 'nuget.exe'
$outDir = Join-Path $root 'dist'

New-Item -ItemType Directory -Path $outDir -Force > $null
Remove-Item -Path (Join-Path $outDir '*') -Force -Recurse
$packages = Get-ChildItem -Path 'src' -Filter *.nuspec -File

# Running nuget pack jobs in parallel to save 10 seconds in total execution time.
# Start-Job occasionally failed with "Remote transport error", so we are using Start-Process as a more robust option.
$procs = foreach ($pkg in $packages) {
    $argList = @('pack', """$($pkg.FullName)""", '-OutputDirectory', """$($outDir)""", '-NonInteractive')
    Write-Host "$nuget $argList"
    Start-Process -FilePath $nuget -ArgumentList $argList -WorkingDirectory $root -NoNewWindow -PassThru |
        Add-Member -NotePropertyName Package -NotePropertyValue $pkg.FullName -PassThru
}

$procs | Wait-Process
$failed = $procs | Where-Object { $_.ExitCode -ne 0 }
if ($failed) {
    $list = $failed | ForEach-Object { "`n - $($_.Package) (exit $($_.ExitCode))" } | Out-String
    throw "NuGet pack failed. Check the output above for errors. Failed packages:$list"
}
