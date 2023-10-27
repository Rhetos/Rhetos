@echo off
setlocal ENABLEDELAYEDEXPANSION
pushd %~dp0..\..
echo Checking *.cs files recursively from %~dp0..\.. for license notice.
echo.
set "FilesMissingLicense=0"
for /R %%f in (*.cs) do (
    set "filepath=%%f"
    set "skiprule=0"
    
    set "replaced=x!filepath:\obj\=!"
    if not "!replaced!"=="x!filepath!" set /A skiprule+=1
    
    set "replaced=x!filepath:\bin\=!"
    if not "!replaced!"=="x!filepath!" set /A skiprule+=1

    set "replaced=x!filepath:\GeneratedFilesCache\=!"
    if not "!replaced!"=="x!filepath!" set /A skiprule+=1

    if !skiprule!==0 (
        findstr /L /M /C:"under the terms of the GNU Affero General Public License" "!filepath!" >nul
        if errorlevel 1 (
            set /A FilesMissingLicense+=1
            echo Source file missing license notice: "!filepath!""
        )
    )
)

echo.
echo Total files missing license: !FilesMissingLicense!
echo.

if not !FilesMissingLicense!==0 (
    popd
    exit 1
)

exit 0
popd

