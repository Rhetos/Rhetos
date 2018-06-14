@REM HINT: SET SECOND ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

CALL Tools\Build\FindVisualStudio.bat || GOTO Error0

@REM Find all test projects, and execute tests for each one:
FOR /F "tokens=*" %%a IN ('DIR "*.Test.dll" /s/b ^| FINDSTR /I ^"\\bin\\%Config%\\^"') DO vstest.console.exe "%%a" || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
