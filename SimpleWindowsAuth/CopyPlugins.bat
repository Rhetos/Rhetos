ECHO Target folder = [%1]
ECHO $(ConfigurationName) = [%2]

SET ThisScriptFolder="%~dp0"

XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.SimpleWindowsAuth\bin\%2\Rhetos.SimpleWindowsAuth.??? %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Security.Service\bin\%2\Rhetos.Security.Service.??? %1 || EXIT /B 1

EXIT /B 0
