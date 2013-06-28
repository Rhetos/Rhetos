@echo off

if [%4]==[] goto usage

cd bin

@echo ---- CreateAndSetDatabase ----
@echo.

CALL "CreateAndSetDatabase.exe" %3 %4  || EXIT /B 1

@echo.
@echo ---- ApplyPackages ----
@echo.

cd ..

IF NOT EXIST ApplyPackages.txt XCOPY Template.ApplyPackages.txt ApplyPackages.txt  || EXIT /B 1

CALL "ApplyPackages.bat"  || EXIT /B 1

cd bin

@echo.
@echo ---- CreateIISExpressSite ----
@echo.

CALL "CreateIISExpressSite.exe" %1 %2  || EXIT /B 1

cd ..

@echo.
@echo ---- Finished ----
@echo.

@echo You can start Rhetos application on IIS Express with following command.
@echo CALL "<Program Files>\IIS Express\IISExpress.exe" /config:IISExpress.config

goto :eof
:usage
@echo Usage: %0 ^<IISWebSiteName^> ^<IISWebSitePort^> ^<SQLServer^> ^<DatabaseName^> 
@echo     ^<IISWebSiteName^> - irrelevant - description name for web site on IIS Express
exit /B 1