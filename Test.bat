SETLOCAL

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

TITLE Testing Rhetos.sln
@REM Using "no-build" option as optimization, because Test.bat should always be executed after Build.bat.
dotnet test Rhetos.sln --no-build || GOTO Error0

IF NOT EXIST test\CommonConcepts.TestApp\local.settings.json ECHO Missing local.settings.json. Follow the Initial setup instructions in Readme.md. & GOTO Error0

CALL tools\Build\TestAppWithDatabase.bat Rhetos.MsSql %Config% || GOTO Error0
CALL tools\Build\TestAppWithDatabase.bat Rhetos.MsSqlEf6 %Config% || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
