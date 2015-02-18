@ECHO OFF

FOR /R Tests %%f IN (*.rhe) DO CALL:TEST "%%f"

ECHO DONE.
PAUSE
EXIT /B 0

:Test

ECHO.
ECHO ==================================
ECHO RUNNING TEST: %1
ECHO.

IF EXIST bin\DslScripts RD bin\DslScripts /S /Q
REM Allow Windows Explorer to release file locks.
IF EXIST bin\DslScripts RD bin\DslScripts /S /Q
IF EXIST bin\DslScripts RD bin\DslScripts /S /Q
IF EXIST bin\DslScripts ECHO ERROR: Cannot clear LOCKED folder 'bin\DslScripts' & GOTO Error1
MD bin\DslScripts
XCOPY %1 bin\DslScripts\ || GOTO Error1

PUSHD bin\Debug
DeployPackages.exe || GOTO Error1
POPD

ECHO ==== TEST PASSED: %~nx1
EXIT /B 0

:Error1
POPD

:Error
ECHO ==== TEST FAILED: %~nx1
PAUSE
EXIT /B 1
