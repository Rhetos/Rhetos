@REM HINT: SET SECOND ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

@IF DEFINED VisualStudioVersion GOTO SkipVcvarsall
@SET VSTOOLS=
@IF "%VS100COMNTOOLS%" NEQ "" SET VSTOOLS=%VS100COMNTOOLS%
@IF "%VS110COMNTOOLS%" NEQ "" SET VSTOOLS=%VS110COMNTOOLS%
CALL "%VSTOOLS%\..\..\VC\vcvarsall.bat" x86 || GOTO Error0
@ECHO ON
:SkipVcvarsall

IF EXIST Build.log DEL Build.log || GOTO Error0
DevEnv.exe "Rhetos.sln" /build %Config% /out Build.log || TYPE Build.log && GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
