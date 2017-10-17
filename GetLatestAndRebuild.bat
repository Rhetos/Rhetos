@REM HINT: SET SECOND ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

git pull || echo CANNOT AUTOMATICALLY GET LATEST VERSION. IT IS POSSIBLE THAT A MANUAL MERGE IS NEEDED. && GOTO Error0
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
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
