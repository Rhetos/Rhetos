@REM Delete all "bin", "obj" and "TestResults" subfolders:
powershell "Get-ChildItem -Path '%~dp0' -Recurse -Directory -Filter 'bin' | ForEach-Object { echo \"Deleting $($_.FullName)\"; Remove-Item $_.FullName -Recurse -Force }"
powershell "Get-ChildItem -Path '%~dp0' -Recurse -Directory -Filter 'obj' | ForEach-Object { echo \"Deleting $($_.FullName)\"; Remove-Item $_.FullName -Recurse -Force }"
powershell "Get-ChildItem -Path '%~dp0' -Recurse -Directory -Filter 'TestResults' | ForEach-Object { echo \"Deleting $($_.FullName)\"; Remove-Item $_.FullName -Recurse -Force }"

@REM Delete build log:
IF EXIST msbuild.log DEL msbuild.log

@REM Delete build installation result from an old version:
IF EXIST Install DEL /F/S/Q Install\*.* >nul
IF EXIST Install RD Install

@REM Delete build installation result:
IF EXIST dist DEL /F/S/Q dist\*.* >nul
IF NOT EXIST dist MD dist
