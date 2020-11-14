SETLOCAL
SET Version=4.3.0
SET Prerelease=auto

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

REM Updating the build version of all projects.
PowerShell -ExecutionPolicy ByPass .\Tools\Build\ChangeVersion.ps1 %Version% %Prerelease% || GOTO Error0

dotnet build "Rhetos.sln" /target:rebuild /p:Configuration=%Config% /verbosity:minimal /fileLogger || GOTO Error1
CALL CreateInstallationPackage.bat %Config% || GOTO Error1

dotnet build CommonConceptsTest.sln /target:restore /p:RestoreForce=True  /target:rebuild /p:Configuration=Debug /verbosity:minimal || GOTO Error1

REM Updating the build version back to "dev" (internal development build), to avoid spamming git history with timestamped prerelease versions.
PowerShell -ExecutionPolicy ByPass .\Tools\Build\ChangeVersion.ps1 %Version% dev || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error1
@PowerShell -ExecutionPolicy ByPass .\Tools\Build\ChangeVersion.ps1 %Version% dev >nul
:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
