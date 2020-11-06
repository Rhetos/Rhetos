@REM HINT: SET SECOND ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

CALL Tools\Build\FindVisualStudio.bat || GOTO Error0

@REM Find all test projects, and execute tests for each one:
vstest.console.exe "Source\Rhetos.Configuration.Autofac.Test\bin\Debug\netcoreapp3.1\Rhetos.Configuration.Autofac.Test.dll" || GOTO Error0
vstest.console.exe "Source\Rhetos.DatabaseGenerator.Test\bin\Debug\netcoreapp3.1\Rhetos.DatabaseGenerator.Test.dll" || GOTO Error0
vstest.console.exe "Source\Rhetos.Dsl.Test\bin\Debug\netcoreapp3.1\Rhetos.Dsl.Test.dll" || GOTO Error0
vstest.console.exe "Source\Rhetos.Extensibility.Test\bin\Debug\netcoreapp3.1\Rhetos.Extensibility.Test.dll" || GOTO Error0
vstest.console.exe "Source\Rhetos.Logging.Test\bin\Debug\netcoreapp3.1\Rhetos.Logging.Test.dll" || GOTO Error0
vstest.console.exe "Source\Rhetos.Persistence.Test\bin\Debug\netcoreapp3.1\Rhetos.Persistence.Test.dll" || GOTO Error0
vstest.console.exe "Source\Rhetos.Utilities.Test\bin\Debug\netcoreapp3.1\Rhetos.Utilities.Test.dll" || GOTO Error0
vstest.console.exe "Source\Rhetos.Deployment.Test\bin\Debug\netcoreapp3.1\Rhetos.Deployment.Test.dll" || GOTO Error0
vstest.console.exe "CommonConcepts\CommonConcepts.Test\bin\Debug\netcoreapp3.1\CommonConcepts.Test.dll" || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
