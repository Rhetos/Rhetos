REM ========================
REM This script is used to prepare testing environment.
REM This script is executed automatically in post build event, while build the Visual Studio project.
REM ========================

SET Config=%1%
IF [%1] == [] SET Config=Debug

REM Set current working folder to folder where this script is, to ensure that the relative paths in this script are valid.
SET ThisScriptFolder="%~dp0"
PUSHD %ThisScriptFolder%

SET BinFolder="%CD%\bin\%Config%"

REM ======================== RHETOS SERVER CONNECTION STRING ==============================

SET ConnectionStringConfigPath=..\Rhetos\bin\ConnectionStrings.config
IF EXIST %ConnectionStringConfigPath% XCOPY /Y/D/R %ConnectionStringConfigPath% %BinFolder%\ || EXIT /B 1

REM ========================
POPD
