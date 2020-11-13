@REM HINT: SET SECOND ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

@REM Find all test projects, and execute tests for each one:
@REM The platform switch is passed so that the test project behaves the same as in Visual Studio
dotnet test "Source\Rhetos.Utilities.Test\bin\Debug\net5.0\Rhetos.Utilities.Test.dll" /Platform:x86 || GOTO Error0
dotnet test "Source\Rhetos.Persistence.Test\bin\Debug\net5.0\Rhetos.Persistence.Test.dll" /Platform:x86 || GOTO Error0
dotnet test "Source\Rhetos.Configuration.Autofac.Test\bin\Debug\net5.0\Rhetos.Configuration.Autofac.Test.dll" /Platform:x86 || GOTO Error0
dotnet test "Source\Rhetos.DatabaseGenerator.Test\bin\Debug\net5.0\Rhetos.DatabaseGenerator.Test.dll" /Platform:x86 || GOTO Error0
dotnet test "Source\Rhetos.Dsl.Test\bin\Debug\net5.0\Rhetos.Dsl.Test.dll" /Platform:x86 || GOTO Error0
dotnet test "Source\Rhetos.Extensibility.Test\bin\Debug\net5.0\Rhetos.Extensibility.Test.dll" /Platform:x86 || GOTO Error0
dotnet test "Source\Rhetos.Logging.Test\bin\Debug\net5.0\Rhetos.Logging.Test.dll" /Platform:x86 || GOTO Error0
dotnet test "Source\Rhetos.Deployment.Test\bin\Debug\net5.0\Rhetos.Deployment.Test.dll" /Platform:x86 || GOTO Error0
dotnet test "CommonConcepts\CommonConcepts.Test\bin\Debug\net5.0\CommonConcepts.Test.dll" /Platform:x86 || GOTO Error0

CALL Tools\Build\FindVisualStudio.bat || GOTO Error0
@REM We are tsestin if the CommonConceptsTest.sln could be restored with the MSBuild command
@REM because the target frameworks used to run custom MSBuild tasks are different when using dotnet cli and MSBuild.exe
MSBuild.exe CommonConceptsTest.sln /target:restore /p:RestoreForce=True  /target:rebuild /p:Configuration=Debug /verbosity:minimal || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
