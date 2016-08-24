@REM HINT: SET THE FIRST ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.
@SETLOCAL
@SETLOCAL ENABLEDELAYEDEXPANSION
@SET "params=;%~1;%~2;%~3;%~4;%~5;%~6;%~7;%~8;%~9;"
@REM //////////////////////////////////////////////////////
SET BuildVersion=1.0.1
SET PrereleaseVersion=auto
@REM SET PrereleaseVersion TO EMPTY VALUE FOR THE OFFICIAL RELEASE.
@REM SET PrereleaseVersion TO "auto" FOR AUTOMATIC PRERELEASE NAME "aph<date and time><last commit hash>"
@REM \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\

IF /I "!PrereleaseVersion!" EQU "auto" (
	@REM If the script parameters contain "/RESTORE" switch: Set the PrereleaseVersion to "dev".
	IF "!params:;/RESTORE;=;!" NEQ "!params!" (
		SET "PrereleaseVersion=dev"
	) ELSE (
		SET "PrereleaseVersion=alpha"
		FOR /f "delims=" %%A IN ('powershell get-date -format "{yyMMddHHmm}"') DO SET "PrereleaseVersion=!PrereleaseVersion!%%A"
		FOR /f "delims=" %%A IN ('git rev-parse --short HEAD') DO SET "PrereleaseVersion=!PrereleaseVersion!%%A"
		@REM NuGet limits to 20 characters.
		SET "PrereleaseVersion=!PrereleaseVersion:~0,20!"
	)
)

@SET PrereleaseVersion

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
