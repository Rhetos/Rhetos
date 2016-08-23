@REM HINT: SET SECOND ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

@IF DEFINED VisualStudioVersion GOTO SkipVcvarsall
IF "%VS140COMNTOOLS%" NEQ "" CALL "%VS140COMNTOOLS%VsDevCmd.bat" x86 && GOTO EndVcvarsall || GOTO Error0
IF "%VS120COMNTOOLS%" NEQ "" CALL "%VS120COMNTOOLS%\..\..\VC\vcvarsall.bat" x86 && GOTO EndVcvarsall || GOTO Error0
IF "%VS110COMNTOOLS%" NEQ "" CALL "%VS110COMNTOOLS%\..\..\VC\vcvarsall.bat" x86 && GOTO EndVcvarsall || GOTO Error0
IF "%VS100COMNTOOLS%" NEQ "" CALL "%VS100COMNTOOLS%\..\..\VC\vcvarsall.bat" x86 && GOTO EndVcvarsall || GOTO Error0
ECHO ERROR: Cannot detect Visual Studio, missing VSxxxCOMNTOOLS variable.
GOTO Error0
:EndVcvarsall
@ECHO ON
:SkipVcvarsall

@IF EXIST Test.log DEL Test.log
@DATE /T >> Test.log
@TIME /T >> Test.log

IF NOT EXIST TestResults MD TestResults

@REM For each test project CALL:TEST
DIR "*.Test.dll" /s/b | FINDSTR /I "\\bin\\%Config%\\" | FINDSTR /I /V "\\TestResults\\" > AllTestProjects.txt
FOR /F "delims=;" %%P IN ('TYPE AllTestProjects.txt') DO @CALL:TEST "%%P"
DEL AllTestProjects.txt

@ECHO. >> Test.log
@DATE /T >> Test.log
@TIME /T >> Test.log

@REM Error analysis:
FINDSTR /N /I /R "\<error\> \<errors\> \<fail\> \<failed\> \<skipped\> \<not.found\>" Test.log | FINDSTR /I /R /V "\<0.error \<0.fail TestCaseManagement.QualityToolsPackage error.ico Warning:" > TestErrors.log

FOR /F "usebackq" %%A IN ('TestErrors.log') DO SET Size=%%~zA
IF 0%Size% GTR 0 TYPE TestErrors.log && GOTO Error0
@SET Size=

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1

:TEST

@ECHO TESTING %~nx1:
@ECHO. >> Test.log
@ECHO == %1 == >> Test.log
MSTest /testcontainer:%1 /runconfig:"%~dp0Local.testsettings" >> Test.log
@REM Wait a second to make sure the .trx file with the next test results has a unique name (filename contains current time)
@TIMEOUT /T 1 /NOBREAK 1> NUL 2> NUL
