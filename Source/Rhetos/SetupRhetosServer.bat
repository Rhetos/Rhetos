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

@SET IISPF=C:\Program Files
@IF "%ProgramFiles%" NEQ "" SET IISPF=%ProgramFiles%
@IF "%ProgramFiles(x86)%" NEQ "" SET IISPF=%ProgramFiles(x86)%

@echo You can start Rhetos application on IIS Express with following command.
@echo CALL "%IISPF%\IIS Express\IISExpress.exe" /config:IISExpress.config

EXIT /B 0

:usage
@echo Usage: %0 ^<WebsiteName^> ^<Port^> ^<SqlServer^> ^<DatabaseName^>
@echo     ^<WebsiteName^> - name of website in IISExpress config (choose any name)
@echo     ^<Port^> - port that Rhetos web service will be listening to if using IIS Express (1234, for example)
@echo     ^<SqlServer^> - Microsoft SQL Server on which there is/will be database for Rhetos server
@echo     ^<DatabaseName^> - name of database that will Rhetos use, script will create database if it doesn't exist.

EXIT /B 1

:Error1
@POPD
:Error0
@ECHO SetupRhetosServer FAILED.
@EXIT /B 1
