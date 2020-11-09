SETLOCAL
SET Version=4.3.0
SET Prerelease=auto

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

REM Updating the build version of all projects.
PowerShell -ExecutionPolicy ByPass .\Tools\Build\ChangeVersion.ps1 %Version% %Prerelease% || GOTO Error0

dotnet build "Rhetos.sln" /target:rebuild /p:Configuration=%Config% /verbosity:minimal /fileLogger || GOTO Error0
CALL CreateInstallationPackage.bat %Config% /NOPAUSE || GOTO Error0

dotnet build CommonConceptsTest.sln /target:restore /p:RestoreForce=True  /target:rebuild /p:Configuration=Debug /verbosity:minimal || GOTO Error0

REM Updating the build version back to "dev" (internal development build), to avoid spamming git history with timestamped prerelease versions.
PowerShell -ExecutionPolicy ByPass .\Tools\Build\ChangeVersion.ps1 %Version% dev || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
