@echo off

if [%4]==[] goto usage

PUSHD bin

@echo ---- CreateAndSetDatabase ----
@echo.

CALL "CreateAndSetDatabase.exe" %3 %4  || GOTO Error1

@echo.
@echo ---- ApplyPackages ----
@echo.

POPD

IF NOT EXIST ApplyPackages.txt COPY Template.ApplyPackages.txt ApplyPackages.txt  || GOTO Error0

CALL "ApplyPackages.bat"  || GOTO Error0

PUSHD bin

@echo.
@echo ---- CreateIISExpressSite ----
@echo.

CALL "CreateIISExpressSite.exe" %1 %2  || GOTO Error1

POPD

@echo.
@echo ---- Finished ----
@echo.

@echo You can start Rhetos application on IIS Express with following command.
@echo CALL "<Program Files>\IIS Express\IISExpress.exe" /config:IISExpress.config

EXIT /B 0

:usage
@echo Usage: %0 ^<IISWebSiteName^> ^<IISWebSitePort^> ^<SQLServer^> ^<DatabaseName^> 
@echo     ^<IISWebSiteName^> - irrelevant - description name for web site on IIS Express
EXIT /B 1

:Error1
@POPD
:Error0
@ECHO SetupRhetosServer FAILED.
@EXIT /B 1
