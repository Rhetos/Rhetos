SETLOCAL

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

@REM Using "no-build" option as optimization, because Test.bat should always be executed after Build.bat.
dotnet test Rhetos.sln --no-build || GOTO Error0

@REM Using "RestoreForce" to make sure that the new version of local Rhetos NuGet packages are included in build.
IF NOT EXIST CommonConcepts\CommonConcepts.Test\rhetos-app.local.settings.json COPY Tools\Configuration\Template.rhetos-app.local.settings.json CommonConcepts\CommonConcepts.Test\rhetos-app.local.settings.json
dotnet build CommonConceptsTest.sln /t:restore /p:RestoreForce=True /t:rebuild --configuration %Config% || GOTO Error0
dotnet test CommonConceptsTest.sln --no-build || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
