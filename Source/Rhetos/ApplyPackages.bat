REM     THIS SCRIPT SHOULD BE USED IN DEVELOPMENT ENVIRONMENT ONLY!
REM     IN PRODUCTION ENVIRONMENT AND QA ENVIRONMENT USE DeployPackages.exe.


REM     HINT: USE COMMAND LINE "ApplyPackages.bat || pause" TO PAUSE ON ERROR.


SET Config=%1%
IF [%1] == [] SET Config=Debug

REM Set current working folder to folder where this script is, to ensure that the relative paths in this script are valid.
SET ThisScriptFolder="%~dp0"
PUSHD %ThisScriptFolder%

SET BinFolder="%CD%\bin"
SET PluginsFolder="%CD%\bin\Plugins"
SET ResourcesFolder="%CD%\Resources"
SET DslScriptsFolder="%CD%\DslScripts"
SET DataMigrationFolder="%CD%\DataMigration"

@REM ======================== IF DB CONNECTION STRING ISN'T DEFINED, JUST EXIT ==============================

IF NOT EXIST bin\ConnectionStrings.config ECHO ERROR: bin\ConnectionStrings.config does not exist. Cannot apply packages. & GOTO Error1

@REM ======================== CLEAR OLD PACKAGES ON RHETOS SERVER ==============================

IF NOT EXIST %PluginsFolder% MD %PluginsFolder%
DEL /F /S /Q %PluginsFolder%\*
IF EXIST %PluginsFolder%\*.dll ECHO ERROR: Cannot delete old plugins. The files are probably locked. & GOTO Error1

RD /S /Q %ResourcesFolder%
IF NOT EXIST %ResourcesFolder% MD %ResourcesFolder%
DEL /F /S /Q %ResourcesFolder%\*

RD /S /Q %DslScriptsFolder%
IF NOT EXIST %DslScriptsFolder% MD %DslScriptsFolder%
DEL /F /S /Q %DslScriptsFolder%\*

RD /S /Q %DataMigrationFolder%
IF NOT EXIST %DataMigrationFolder% MD %DataMigrationFolder%
DEL /F /S /Q %DataMigrationFolder%\*

@REM ======================== FOR SELECTED PACKAGES COPY BUILD OUTPUT TO RHETOS SERVER ==============================

IF NOT EXIST ApplyPackages.txt ECHO ERROR: List of packages is not defined, won't apply packages. Each line in ApplyPackages.txt should contain relative path to a selected package folder, for example "..\..\CommonConcepts". & GOTO Error1

FOR /F "delims=;" %%P IN ('TYPE ApplyPackages.txt') DO CALL:CopyPackageOutput "%%P" || GOTO Error1
GOTO Continue1

:CopyPackageOutput
ECHO === Copying package from %1 ===
IF [%~nx1]==[] ECHO ERROR: Package path must not end with a backslash ('\'). Package patch: %1 & EXIT /B 1
IF NOT EXIST "%~1" ECHO ERROR: Package directory does not exist: '%~dpnx1' & EXIT /B 1
IF EXIST "%~1\CopyPlugins.bat" CALL "%~1\CopyPlugins.bat" %PluginsFolder% %Config% || EXIT /B 1
IF EXIST "%~1\Resources\" XCOPY /Y/D/R "%~1\Resources\*.*" %ResourcesFolder%\%~nx1\ || EXIT /B 1
IF EXIST "%~1\DslScripts\" XCOPY /Y/D/R /S "%~1\DslScripts\*.*" %DslScriptsFolder%\%~nx1\ || EXIT /B 1
IF EXIST "%~1\DataMigration\" XCOPY /Y/D/R /S "%~1\DataMigration\*.sql" %DataMigrationFolder%\%~nx1\ || EXIT /B 1
EXIT /B

:Continue1

@REM ======================== PRECOMPILED DOMAIN OBJECT MODEL ==============================

DEL /F /S /Q ServerDom.??? || GOTO Error1
PUSHD %BinFolder%
DeployPackages.exe || GOTO Error2
POPD

@REM ========================
@POPD
@EXIT /B 0

:Error2
@POPD
:Error1
@POPD
@ECHO APPLYPACKAGES FAILED.
@EXIT /B 1
