ECHO Target folder = [%1]
ECHO $(ConfigurationName) = [%2]

REM "%~dp0" is this script's folder.

XCOPY /Y/D/R "%~dp0"Plugins\Rhetos.SimpleWindowsAuth\bin\%2\Rhetos.SimpleWindowsAuth.??? %1 || EXIT /B 1
XCOPY /Y/D/R "%~dp0"Plugins\Rhetos.Security.Service\bin\%2\Rhetos.Security.Service.??? %1 || EXIT /B 1

EXIT /B 0
