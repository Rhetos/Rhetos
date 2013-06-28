@ECHO OFF

SET Config=%1%
IF [%1] == [] SET Config=Debug

SET LogFile=Build.log
IF EXIST %LogFile% DEL %LogFile%
DATE /T >> %LogFile%
TIME /T >> %LogFile%

SET VSTOOLS=
IF "%VS100COMNTOOLS%" NEQ "" SET VSTOOLS=%VS100COMNTOOLS%
IF "%VS110COMNTOOLS%" NEQ "" SET VSTOOLS=%VS110COMNTOOLS%
CALL "%VSTOOLS%\..\..\VC\vcvarsall.bat" x86 2>> %LogFile%

CALL:BUILD Rhetos.sln

ECHO. >> %LogFile%
DATE /T >> %LogFile%
TIME /T >> %LogFile%

REM Error analysis:
FINDSTR /N /I /R "\<error\> \<errors\> \<fail\> \<failed\> \<skipped\>" %LogFile% | FINDSTR /I /R /V "\<0.error \<0.fail TestCaseManagement.QualityToolsPackage error.ico Warning:" > BuildErrors.log

EXIT /B 0


:BUILD

SET Title=BUILDING SOLUTION %1
ECHO %Title%
ECHO. >> %LogFile%
ECHO %Title% >> %LogFile%

DevEnv.exe "%1" /build %Config% /out %LogFile%
