ECHO Target folder = [%1]
ECHO $(ConfigurationName) = [%2]

SET ThisScriptFolder=%~dp0

XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Dsl.DefaultConcepts\bin\%2\Rhetos.Dsl.DefaultConcepts.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Dsl.DefaultConcepts\bin\%2\Rhetos.Dsl.DefaultConcepts.pdb %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Dom.DefaultConcepts\bin\%2\Rhetos.Dom.DefaultConcepts.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Dom.DefaultConcepts\bin\%2\Rhetos.Dom.DefaultConcepts.pdb %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Dom.DefaultConcepts.Interfaces\bin\%2\Rhetos.Dom.DefaultConcepts.Interfaces.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Dom.DefaultConcepts.Interfaces\bin\%2\Rhetos.Dom.DefaultConcepts.Interfaces.pdb %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.DatabaseGenerator.DefaultConcepts\bin\%2\Rhetos.DatabaseGenerator.DefaultConcepts.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.DatabaseGenerator.DefaultConcepts\bin\%2\Rhetos.DatabaseGenerator.DefaultConcepts.pdb %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Persistence.NHibernateDefaultConcepts\bin\%2\Rhetos.Persistence.NHibernateDefaultConcepts.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Persistence.NHibernateDefaultConcepts\bin\%2\Rhetos.Persistence.NHibernateDefaultConcepts.pdb %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Processing.DefaultCommands\bin\%2\Rhetos.Processing.DefaultCommands.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Processing.DefaultCommands\bin\%2\Rhetos.Processing.DefaultCommands.pdb %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Processing.DefaultCommands.Interfaces\bin\%2\Rhetos.Processing.DefaultCommands.Interfaces.dll %1 || EXIT /B 1
XCOPY /Y/D/R %ThisScriptFolder%Plugins\Rhetos.Processing.DefaultCommands.Interfaces\bin\%2\Rhetos.Processing.DefaultCommands.Interfaces.pdb %1 || EXIT /B 1

EXIT /B 0
