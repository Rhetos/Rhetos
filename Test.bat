SETLOCAL

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

@REM Using "no-build" option as optimization, because Test.bat should always be executed after Build.bat.
dotnet test Rhetos.sln --no-build || GOTO Error0

IF NOT EXIST test\CommonConcepts.TestApp\local.settings.json ECHO Missing local.settings.json. Follow the Initial setup instructions in Readme.md. & GOTO Error0

CALL :TestAppWithDatabase Rhetos.MsSqlEf6 || GOTO Error0
CALL :TestAppWithDatabase Rhetos.MsSql || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1

@REM ================================================
:TestAppWithDatabase

TITLE Testing %1
dotnet remove test\CommonConcepts.TestApp package Rhetos.MsSql > nul
dotnet remove test\CommonConcepts.TestApp package Rhetos.MsSqlEf6 > nul
dotnet add test\CommonConcepts.TestApp package %1 --version 6.0.0-dev* || GOTO DatabaseError0

@REM Using "RestoreForce" to make sure that the new version of local Rhetos NuGet packages are included in build.
dotnet build CommonConceptsTest.sln /t:restore /p:RestoreForce=True /t:rebuild --configuration %Config% || GOTO DatabaseError0
@REM Running dbupdate again to test the CLI (it was executed in the build above).
dotnet test\CommonConcepts.TestApp\bin\Debug\net8.0\rhetos.dll dbupdate test\CommonConcepts.TestApp\bin\Debug\net8.0\CommonConcepts.TestApp.dll || GOTO DatabaseError0
dotnet test CommonConceptsTest.sln --no-build || GOTO DatabaseError0

IF EXIST "%ProgramFiles%\LINQPad8\LPRun8.exe" "%ProgramFiles%\LINQPad8\LPRun8.exe" "test\CommonConcepts.TestApp\bin\Debug\net8.0\LinqPad\Rhetos DOM.linq" > nul || GOTO DatabaseError0

@EXIT /B 0

:DatabaseError0
@ECHO.
@ECHO Error while testing with '%1'.
@EXIT /B 1
