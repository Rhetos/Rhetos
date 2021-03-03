SETLOCAL

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

CALL Clean.bat || GOTO Error0
CALL Build.bat %Config% /NOPAUSE || GOTO Error0
CALL Test.bat %Config% /NOPAUSE || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
