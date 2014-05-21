SET Config=%1%
IF [%1] == [] SET Config=Debug

REM Set current working folder to folder where this script is, to ensure that the relative paths in this script are valid.
PUSHD "%~dp0"

SET BinFolder="%CD%\bin"
SET PluginsFolder="%CD%\Plugins"
SET ResourcesFolder="%CD%\Resources"
SET DslScriptsFolder="%CD%\DslScripts"

REM ======================== DEPLOYMENT TOOLS ==============================

XCOPY /Y/D/R ..\..\Source\CreateIISExpressSite\bin\%Config%\CreateIISExpressSite.??? %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\CreateIISExpressSite\bin\%Config%\CreateIISExpressSite.exe.config %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\CreateIISExpressSite\bin\%Config%\*.dll %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\CreateIISExpressSite\bin\%Config%\*.pdb %BinFolder%

XCOPY /Y/D/R ..\..\Source\CreateAndSetDatabase\bin\%Config%\CreateAndSetDatabase.??? %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\CreateAndSetDatabase\bin\%Config%\CreateAndSetDatabase.exe.config %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\CreateAndSetDatabase\bin\%Config%\*.dll %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\CreateAndSetDatabase\bin\%Config%\*.pdb %BinFolder%

XCOPY /Y/D/R ..\..\Source\DeployPackages\bin\%Config%\DeployPackages.??? %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\DeployPackages\bin\%Config%\DeployPackages.exe.config %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\DeployPackages\bin\%Config%\*.dll %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\DeployPackages\bin\%Config%\*.pdb %BinFolder%

XCOPY /Y/D/R ..\..\Source\ExtractPackages\bin\%Config%\ExtractPackages.??? %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\ExtractPackages\bin\%Config%\*.dll %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\ExtractPackages\bin\%Config%\*.pdb %BinFolder%

XCOPY /Y/D/R ..\..\Source\CleanupOldData\bin\%Config%\CleanupOldData.??? %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\CleanupOldData\bin\%Config%\CleanupOldData.exe.config %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\CleanupOldData\bin\%Config%\*.dll %BinFolder% || EXIT /B 1
XCOPY /Y/D/R ..\..\Source\CleanupOldData\bin\%Config%\*.pdb %BinFolder%

REM ========================
POPD

ECHO Done.
