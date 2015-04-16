@REM HINT: SET THE FIRST ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.
@SETLOCAL
@REM //////////////////////////////////////////////////////
SET BuildVersion=0.9.32
SET PrereleaseVersion=alpha001
@REM SET PrereleaseVersion TO EMPTY VALUE FOR THE OFFICIAL RELEASE.
@REM \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\

PUSHD "%~dp0" || GOTO Error0
CALL ChangeRhetosPackageVersion.bat Source %BuildVersion% %PrereleaseVersion% || GOTO Error1
CALL ChangeRhetosPackageVersion.bat CommonConcepts %BuildVersion% %PrereleaseVersion% || GOTO Error1
CALL ChangeRhetosPackageVersion.bat CommonConcepts\CommonConceptsTest %BuildVersion% %PrereleaseVersion% || GOTO Error1
CALL ChangeRhetosPackageVersion.bat SimpleWindowsAuth %BuildVersion% %PrereleaseVersion% || GOTO Error1
CALL ChangeRhetosPackageVersion.bat AspNetFormsAuth %BuildVersion% %PrereleaseVersion% || GOTO Error1
CALL ChangeRhetosPackageVersion.bat ActiveDirectorySync %BuildVersion% %PrereleaseVersion% || GOTO Error1
@POPD

@REM ================================================
:Done
@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0
:Error1
@POPD
:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%1] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
