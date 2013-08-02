ECHO Target folder = [%1]
ECHO $(ConfigurationName) = [%2]

SET ThisScriptFolder=%~dp0

XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.MvcModelGenerator\bin\%2\Rhetos.MvcModelGenerator.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.MvcModelGenerator\bin\%2\Rhetos.MvcModelGenerator.pdb %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.MvcModelGenerator.DefaultConcepts\bin\%2\Rhetos.MvcModelGenerator.DefaultConcepts.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.MvcModelGenerator.DefaultConcepts\bin\%2\Rhetos.MvcModelGenerator.DefaultConcepts.pdb %1 || EXIT /B 1

EXIT /B 0
