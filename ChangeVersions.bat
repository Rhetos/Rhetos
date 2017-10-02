@REM COMMAND LINE ARGUMENTS:
@REM /NOPAUSE   SCRIT EXITS ON ERROR, WITHOUT WAITING FOR USER'S RESPONSE. USE THIS WHEN AUTOMATING THE BUILD.
@REM /RESTORE   SETS THE PROJECT'S VERSION TO "dev" PRERELESE. USE THIS BEFORE COMMITTING TO SOURCE REPOSTIORY.

@SETLOCAL
@SETLOCAL ENABLEDELAYEDEXPANSION
@SET "params=;%~1;%~2;%~3;%~4;%~5;%~6;%~7;%~8;%~9;"
@REM //////////////////////////////////////////////////////
SET BuildVersion=2.1.0
SET PrereleaseVersion=auto
@REM SET PrereleaseVersion TO EMPTY VALUE FOR THE OFFICIAL RELEASE.
@REM SET PrereleaseVersion TO "auto" FOR AUTOMATIC PRERELEASE NAME "dev<date and time><last commit hash>"
@REM \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\

IF /I "!PrereleaseVersion!" EQU "auto" (
    @REM If the script parameters contain "/RESTORE" switch: Set the PrereleaseVersion to "dev".
IF "!params:;/RESTORE;=;!" NEQ "!params!" (
    SET "PrereleaseVersion=dev"
) ELSE (
        SET "PrereleaseVersion=dev"
        FOR /f "delims=" %%A IN ('powershell get-date -format "{yyMMddHHmm}"') DO SET "PrereleaseVersion=!PrereleaseVersion!%%A"
        FOR /f "delims=" %%A IN ('git rev-parse --short HEAD') DO SET "PrereleaseVersion=!PrereleaseVersion!%%A"
        @REM NuGet limits to 20 characters.
        SET "PrereleaseVersion=!PrereleaseVersion:~0,20!"
    )
)

ECHO PrereleaseVersion="!PrereleaseVersion!"
PUSHD "%~dp0" || GOTO Error0
CALL ChangeRhetosPackageVersion.bat . %BuildVersion% !PrereleaseVersion! || GOTO Error1
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
@REM If the script parameters do not contain "/NOPAUSE" switch: Execute PAUSE command.
@IF "!params:;/NOPAUSE;=;!" EQU "!params!" @PAUSE
@EXIT /B 1
