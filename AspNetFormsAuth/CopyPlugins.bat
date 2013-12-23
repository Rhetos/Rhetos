ECHO Target folder = [%1]
ECHO $(ConfigurationName) = [%2]

SET ThisScriptFolder="%~dp0"

XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.AspNetFormsAuth\bin\%2\Rhetos.AspNetFormsAuth.??? %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.AspNetFormsAuth\bin\%2\WebMatrix.Data.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.AspNetFormsAuth\bin\%2\WebMatrix.WebData.dll %1 || EXIT /B 1

EXIT /B 0
