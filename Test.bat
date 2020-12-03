SETLOCAL

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

@REM Find all test projects, and execute tests for each one:
@REM The platform switch is passed so that the test project behaves the same as in Visual Studio
dotnet test "Source\Rhetos.Utilities.Test\bin\Debug\net5.0\Rhetos.Utilities.Test.dll" || GOTO Error0
dotnet test "Source\Rhetos.Persistence.Test\bin\Debug\net5.0\Rhetos.Persistence.Test.dll" || GOTO Error0
dotnet test "Source\Rhetos.Configuration.Autofac.Test\bin\Debug\net5.0\Rhetos.Configuration.Autofac.Test.dll" || GOTO Error0
dotnet test "Source\Rhetos.DatabaseGenerator.Test\bin\Debug\net5.0\Rhetos.DatabaseGenerator.Test.dll" || GOTO Error0
dotnet test "Source\Rhetos.Dsl.Test\bin\Debug\net5.0\Rhetos.Dsl.Test.dll" || GOTO Error0
dotnet test "Source\Rhetos.Extensibility.Test\bin\Debug\net5.0\Rhetos.Extensibility.Test.dll" || GOTO Error0
dotnet test "Source\Rhetos.Logging.Test\bin\Debug\net5.0\Rhetos.Logging.Test.dll" || GOTO Error0
dotnet test "Source\Rhetos.Deployment.Test\bin\Debug\net5.0\Rhetos.Deployment.Test.dll" || GOTO Error0
dotnet test "CommonConcepts\Plugins\Rhetos.CommonConcepts.Test\bin\Debug\net5.0\Rhetos.CommonConcepts.Test.dll" || GOTO Error0

dotnet build CommonConceptsTest.sln /target:restore /p:RestoreForce=True /target:rebuild /p:Configuration=Debug /verbosity:minimal || GOTO Error0
dotnet test "CommonConcepts\CommonConcepts.Test\bin\Debug\net5.0\CommonConcepts.Test.dll" || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
