SETLOCAL

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

CALL Clean.bat || GOTO Error0

CALL Build.bat %Config% /NOPAUSE || GOTO Error0

REM Deploying test application for integration tests:
Source\Rhetos\bin\DeployPackages.exe /NoPause /Debug /ShortTransactions || GOTO Error0
CALL Tools\Build\FindVisualStudio.bat || GOTO Error0
NuGet restore CommonConceptsTest.sln
MSBuild.exe CommonConceptsTest.sln /target:rebuild /p:Configuration=Debug /verbosity:minimal || GOTO Error0

CALL Test.bat %Config% /NOPAUSE || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
