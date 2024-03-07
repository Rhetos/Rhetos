SETLOCAL

TITLE Testing %1
@SET Config=%2%
@IF [%2] == [] SET Config=Debug

IF %1 NEQ Rhetos.MsSqlEf6 dotnet remove test\CommonConcepts.TestApp package Rhetos.MsSqlEf6 > nul
IF %1 NEQ Rhetos.MsSql dotnet remove test\CommonConcepts.TestApp package Microsoft.EntityFrameworkCore.Proxies > nul
IF %1 NEQ Rhetos.MsSql dotnet remove test\CommonConcepts.TestApp package Rhetos.MsSql > nul

IF %1 EQU Rhetos.MsSqlEf6 dotnet add test\CommonConcepts.TestApp package Rhetos.MsSqlEf6 --version 6.0.0-dev* || GOTO Error0
IF %1 EQU Rhetos.MsSql dotnet add test\CommonConcepts.TestApp package Rhetos.MsSql --version 6.0.0-dev* || GOTO Error0
IF %1 EQU Rhetos.MsSql dotnet add test\CommonConcepts.TestApp package Microsoft.EntityFrameworkCore.Proxies --version 8.0.* || GOTO Error0

@REM Using "RestoreForce" to make sure that the new version of local Rhetos NuGet packages are included in build.
dotnet build CommonConceptsTest.sln /t:restore /p:RestoreForce=True /t:rebuild --configuration %Config% || GOTO Error0
@REM Running dbupdate again to test the CLI (it was executed in the build above).
dotnet test\CommonConcepts.TestApp\bin\Debug\net8.0\rhetos.dll dbupdate test\CommonConcepts.TestApp\bin\Debug\net8.0\CommonConcepts.TestApp.dll || GOTO Error0
dotnet test CommonConceptsTest.sln --no-build || GOTO Error0

IF EXIST "%ProgramFiles%\LINQPad8\LPRun8.exe" "%ProgramFiles%\LINQPad8\LPRun8.exe" "test\CommonConcepts.TestApp\bin\Debug\net8.0\LinqPad\Rhetos DOM.linq" > nul || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED WITH DB PROVIDER '%1'.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED WITH DB PROVIDER '%1'.
@EXIT /B 1
