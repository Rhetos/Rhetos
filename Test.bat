@REM HINT: SET SECOND ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

CALL Tools\Build\FindVisualStudio.bat || GOTO Error0

REM Running unit tests:
vstest.console.exe CommonConcepts\Plugins\Rhetos.CommonConcepts.Test\bin\Debug\Rhetos.CommonConcepts.Test.dll || GOTO Error0
vstest.console.exe Source\DeployPackages.Test\bin\Debug\DeployPackages.Test.dll || GOTO Error0
vstest.console.exe Source\Rhetos.DatabaseGenerator.Test\bin\Debug\Rhetos.DatabaseGenerator.Test.dll || GOTO Error0
vstest.console.exe Source\Rhetos.Deployment.Test\bin\Debug\Rhetos.Deployment.Test.dll || GOTO Error0
vstest.console.exe Source\Rhetos.Dsl.Test\bin\Debug\Rhetos.Dsl.Test.dll || GOTO Error0
vstest.console.exe Source\Rhetos.Extensibility.Test\bin\Debug\Rhetos.Extensibility.Test.dll || GOTO Error0
vstest.console.exe Source\Rhetos.Logging.Test\bin\Debug\Rhetos.Logging.Test.dll || GOTO Error0
vstest.console.exe Source\Rhetos.Persistence.Test\bin\Debug\Rhetos.Persistence.Test.dll || GOTO Error0
vstest.console.exe Source\Rhetos.Utilities.Test\bin\Debug\Rhetos.Utilities.Test.dll || GOTO Error0
vstest.console.exe Source\Rhetos.Web.Test\bin\Debug\Rhetos.Web.Test.dll || GOTO Error0

REM Deploying test application for integration tests:
Source\Rhetos\bin\DeployPackages.exe /NoPause /Debug /ShortTransactions || GOTO Error0
NuGet restore "CommonConceptsTest.sln" || GOTO Error0
MSBuild.exe "CommonConceptsTest.sln" /t:restore /t:rebuild /p:Configuration=Debug /verbosity:minimal || GOTO Error0

REM Running integration tests:
vstest.console.exe CommonConcepts\CommonConceptsTest\CommonConcepts.Test\bin\Debug\CommonConcepts.Test.dll || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
