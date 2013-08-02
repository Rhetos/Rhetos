IF NOT EXIST Install\ MD Install
DEL /F /S /Q Install\* || EXIT /B 1
IF EXIST Install\*.zip ECHO ERROR: Cannot delete old files in Install folder. Check if the files are locked. & EXIT /B 1
RD /S /Q Install\Rhetos
IF EXIST Install\Rhetos ECHO ERROR: Cannot delete old files in Install folder. Check if the files are locked. & EXIT /B 1

SET Config=%1%
IF [%1] == [] SET Config=Debug

DEL /F /Q *.zip || EXIT /B 1

IF NOT EXIST CommonConcepts\Plugins\ForDeployment\ MD CommonConcepts\Plugins\ForDeployment\
DEL /F /S /Q CommonConcepts\Plugins\ForDeployment\* || EXIT /B 1
CALL CommonConcepts\CopyPlugins.bat CommonConcepts\Plugins\ForDeployment\ %Config%
Source\CreatePackage\bin\%Config%\CreatePackage.exe CommonConcepts
RD /S /Q CommonConcepts\Plugins\ForDeployment\

IF NOT EXIST MvcModelGenerator\Plugins\ForDeployment\ MD MvcModelGenerator\Plugins\ForDeployment\
DEL /F /S /Q MvcModelGenerator\Plugins\ForDeployment\* || EXIT /B 1
CALL MvcModelGenerator\CopyPlugins.bat MvcModelGenerator\Plugins\ForDeployment\ %Config%
Source\CreatePackage\bin\%Config%\CreatePackage.exe MvcModelGenerator
RD /S /Q MvcModelGenerator\Plugins\ForDeployment\

MOVE *.zip Install\

MD Install\Rhetos
MD Install\Rhetos\bin

XCOPY /Y/D/R Source\Rhetos\bin\*.dll Install\Rhetos\bin
XCOPY /Y/D/R Source\Rhetos\bin\*.pdb Install\Rhetos\bin
XCOPY /Y/D/R Source\Rhetos\bin\*.exe Install\Rhetos\bin
XCOPY /Y/D/R Source\Rhetos\bin\*.config Install\Rhetos\bin
DEL /F /Q Install\Rhetos\bin\ConnectionStrings.config

REM Workaround until design issue is resolved: Rhetos framework references CommonConcepts package (Common.Claim)
DEL /F /Q Install\Rhetos\bin\*.Default*.???

XCOPY /S/I /Y/D/R Source\Rhetos\Css Install\Rhetos\Css
XCOPY /S/I /Y/D/R Source\Rhetos\Img Install\Rhetos\Img
XCOPY /S/I /Y/D/R Source\Rhetos\Js Install\Rhetos\Js

XCOPY /Y/D/R Source\Rhetos\*.aspx Install\Rhetos\
XCOPY /Y/D/R Source\Rhetos\*.asax Install\Rhetos\
XCOPY /Y/D/R Source\Rhetos\Web.config Install\Rhetos\
XCOPY /Y/D/R Source\Rhetos\*.linq Install\Rhetos\
XCOPY /Y/D/R Source\Rhetos\*.svc Install\Rhetos\
XCOPY /Y/D/R Source\Rhetos\Site.Master Install\Rhetos\

XCOPY /Y/D/R ChangeLog.md Install\Rhetos\
XCOPY /Y/D/R Readme.md Install\Rhetos\
