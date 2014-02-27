ECHO Target folder = [%1]
ECHO $(ConfigurationName) = [%2]

SET ThisScriptFolder="%~dp0"

XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.AspNetFormsAuth\bin\%2\Rhetos.AspNetFormsAuth.??? %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.AspNetFormsAuth\bin\%2\WebMatrix.Data.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.AspNetFormsAuth\bin\%2\WebMatrix.WebData.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\AdminSetup\bin\%2\AdminSetup.??? %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\AdminSetup\bin\%2\AdminSetup.exe.config %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\InitAspNetDatabase\bin\%2\InitAspNetDatabase.??? %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\InitAspNetDatabase\bin\%2\InitAspNetDatabase.exe.config %1 || EXIT /B 1

REM These dlls are required at runtime (by WebMatrix.WebData), but are not directly referenced in the application's code:
XCOPY /Y/D/R %ThisScriptFolder%Plugins\AdminSetup\bin\%2\System.Web.WebPages.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\AdminSetup\bin\%2\System.Web.Helpers.dll %1 || EXIT /B 1

EXIT /B 0
