SETLOCAL

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

@REM Running unit test that don't use the database. Using "no-build" option as optimization, because Test.bat should always be executed after Build.bat.
dotnet test Rhetos.sln --no-build || GOTO Error0

IF NOT EXIST test\CommonConcepts.TestApp.MsSql\local.settings.json ECHO Missing test\CommonConcepts.TestApp.MsSql\local.settings.json. Follow the Initial setup instructions in Readme.md. & GOTO Error0
IF NOT EXIST test\CommonConcepts.TestApp.MsSqlEf6\local.settings.json ECHO Missing test\CommonConcepts.TestApp.MsSqlEf6\local.settings.json. Follow the Initial setup instructions in Readme.md. & GOTO Error0
IF NOT EXIST test\CommonConcepts.TestApp.PostgreSql\local.settings.json ECHO Missing test\CommonConcepts.TestApp.PostgreSql\local.settings.json. Follow the Initial setup instructions in Readme.md. & GOTO Error0

@REM Using "RestoreForce" to make sure that the new version of local Rhetos NuGet packages are included in build.
dotnet build CommonConceptsTest.sln /t:restore /p:RestoreForce=True /t:rebuild --configuration %Config% || GOTO Error0

@REM Running dbupdate again to test the Rhetos CLI (it was executed in the build above).
dotnet test\CommonConcepts.TestApp.MsSql\bin\Debug\net8.0\rhetos.dll dbupdate test\CommonConcepts.TestApp.MsSql\bin\Debug\net8.0\CommonConcepts.TestApp.MsSql.dll || GOTO Error0
dotnet test\CommonConcepts.TestApp.MsSqlEf6\bin\Debug\net8.0\rhetos.dll dbupdate test\CommonConcepts.TestApp.MsSqlEf6\bin\Debug\net8.0\CommonConcepts.TestApp.MsSqlEf6.dll || GOTO Error0
dotnet test\CommonConcepts.TestApp.PostgreSql\bin\Debug\net8.0\rhetos.dll dbupdate test\CommonConcepts.TestApp.PostgreSql\bin\Debug\net8.0\CommonConcepts.TestApp.PostgreSql.dll || GOTO Error0

@REM Running integration tests that use the database.
dotnet test CommonConceptsTest.sln --no-build || GOTO Error0

IF EXIST "%ProgramFiles%\LINQPad8\LPRun8.exe" "%ProgramFiles%\LINQPad8\LPRun8.exe" "test\CommonConcepts.TestApp.MsSql\bin\Debug\net8.0\LinqPad\Rhetos DOM.linq" > nul || GOTO Error0
IF EXIST "%ProgramFiles%\LINQPad8\LPRun8.exe" "%ProgramFiles%\LINQPad8\LPRun8.exe" "test\CommonConcepts.TestApp.MsSqlEf6\bin\Debug\net8.0\LinqPad\Rhetos DOM.linq" > nul || GOTO Error0
IF EXIST "%ProgramFiles%\LINQPad8\LPRun8.exe" "%ProgramFiles%\LINQPad8\LPRun8.exe" "test\CommonConcepts.TestApp.PostgreSql\bin\Debug\net8.0\LinqPad\Rhetos DOM.linq" > nul || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
