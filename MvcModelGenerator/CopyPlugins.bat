ECHO Target folder = [%1]
ECHO $(ConfigurationName) = [%2]

SET ThisScriptFolder=%~dp0

XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.MvcGenerator\bin\%2\Rhetos.MvcGenerator.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.MvcGenerator\bin\%2\Rhetos.MvcGenerator.pdb %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.MvcGenerator.DefaultConcepts\bin\%2\Rhetos.MvcGenerator.DefaultConcepts.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.MvcGenerator.DefaultConcepts\bin\%2\Rhetos.MvcGenerator.DefaultConcepts.pdb %1 || EXIT /B 1

EXIT /B 0
