REM ========================
REM This script is used to prepare testing environment for DeployPackages.exe.
REM This script is executed automatically in post build event, while build the Visual Studio project.
REM ========================

SET Config=%1%
IF [%1] == [] SET Config=Debug

REM Set current working folder to folder where this script is, to ensure that the relative paths in this script are valid.
SET ThisScriptFolder="%~dp0"
PUSHD %ThisScriptFolder%

SET BinFolder="%CD%\bin\%Config%"
SET PluginsFolder="%CD%\bin\%Config%\Plugins"
SET DslScriptsFolder="%CD%\bin\DslScripts"
SET DataMigrationFolder="%CD%\bin\DataMigration"

REM ======================== PLUGINS ==============================

IF NOT EXIST %PluginsFolder% MD %PluginsFolder%
DEL /F /S /Q %PluginsFolder%\* || EXIT /B 1

CALL ..\..\CommonConcepts\CopyPlugins.bat %PluginsFolder% %Config% || EXIT /B 1

REM ======================== DSL SCRIPTS ==============================

IF NOT EXIST %DslScriptsFolder% MD %DslScriptsFolder%
IF NOT EXIST %DslScriptsFolder%\CommonConcepts MD %DslScriptsFolder%\CommonConcepts
IF NOT EXIST %DslScriptsFolder%\CommonConceptsTest MD %DslScriptsFolder%\CommonConceptsTest
DEL /F /S /Q %DslScriptsFolder%\*

XCOPY /Y/D/R /S ..\..\CommonConcepts\DslScripts\* %DslScriptsFolder%\CommonConcepts || EXIT /B 1
XCOPY /Y/D/R /S ..\..\CommonConcepts\CommonConceptsTest\DslScripts\* %DslScriptsFolder%\CommonConceptsTest || EXIT /B 1

REM ======================== DATA MIGRATION SCRIPTS ==============================

IF NOT EXIST %DataMigrationFolder% MD %DataMigrationFolder%
DEL /F /S /Q %DataMigrationFolder%\*

IF EXIST ..\..\CommonConcepts\DataMigration XCOPY /Y/D/R /S ..\..\CommonConcepts\DataMigration\*.sql %DataMigrationFolder%\CommonConcepts\ || EXIT /B 1

REM ======================== RHETOS SERVER CONNECTION STRING ==============================

SET ConnectionStringConfigPath=..\..\Source\Rhetos\bin\ConnectionStrings.config
IF EXIST %ConnectionStringConfigPath% XCOPY /Y/D/R %ConnectionStringConfigPath% %BinFolder% || EXIT /B 1

REM ========================
POPD
