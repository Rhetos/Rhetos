@ECHO OFF

SET Config=%1%
IF [%1] == [] SET Config=Debug

SET LogFile=Test.log
IF EXIST %LogFile% DEL %LogFile%
DATE /T >> %LogFile%
TIME /T >> %LogFile%

SET VSTOOLS=
IF "%VS100COMNTOOLS%" NEQ "" SET VSTOOLS=%VS100COMNTOOLS%
IF "%VS110COMNTOOLS%" NEQ "" SET VSTOOLS=%VS110COMNTOOLS%
CALL "%VSTOOLS%\..\..\VC\vcvarsall.bat" x86 2>> %LogFile%

IF NOT EXIST TestResults MD TestResults


REM For each test project CALL:TEST
DIR "*.Test.dll" /s/b | FINDSTR /I "\\bin\\%Config%\\" | FINDSTR /I /V "\\TestResults\\" > AllTestProjects.txt
FOR /F "delims=;" %%P IN ('TYPE AllTestProjects.txt') DO CALL:TEST "%%P"
DEL AllTestProjects.txt

ECHO. >> %LogFile%
DATE /T >> %LogFile%
TIME /T >> %LogFile%

REM Error analysis:
FINDSTR /N /I /R "\<error\> \<errors\> \<fail\> \<failed\> \<skipped\>" %LogFile% | FINDSTR /I /R /V "\<0.error \<0.fail TestCaseManagement.QualityToolsPackage error.ico Warning:" > TestErrors.log

EXIT /B 0


:TEST

ECHO == %1 ==
ECHO. >> %LogFile%
ECHO == %1 == >> %LogFile%
MSTest /testcontainer:%1 /runconfig:"Local.testsettings" >> %LogFile%
REM Wait a second to make sure the .trx file with the next test results has a unique name (filename contains current time)
TIMEOUT 1 > nul
