IF NOT EXIST Install\ MD Install
DEL /F /S /Q Install\* || EXIT /B 1
IF EXIST Install\*.zip ECHO ERROR: Cannot delete old files in Install folder. Check if the files are locked. & EXIT /B 1
RD /S /Q Install\Rhetos
IF EXIST Install\Rhetos ECHO ERROR: Cannot delete old files in Install folder. Check if the files are locked. & EXIT /B 1

SET Config=%1%
IF [%1] == [] SET Config=Debug

DEL /F /Q *.zip || EXIT /B 1

IF NOT EXIST CommonConcepts\Plugins\ForDeployment\ MD CommonConcepts\Plugins\ForDeployment\
DEL /F /S /Q CommonConcepts\Plugins\ForDeployment\*  || GOTO Error0
CALL CommonConcepts\CopyPlugins.bat CommonConcepts\Plugins\ForDeployment\ %Config%  || GOTO Error0
Source\CreatePackage\bin\%Config%\CreatePackage.exe CommonConcepts  || GOTO Error0
RD /S /Q CommonConcepts\Plugins\ForDeployment\

IF NOT EXIST SimpleWindowsAuth\Plugins\ForDeployment\ MD SimpleWindowsAuth\Plugins\ForDeployment\
DEL /F /S /Q SimpleWindowsAuth\Plugins\ForDeployment\*  || GOTO Error0
CALL SimpleWindowsAuth\CopyPlugins.bat SimpleWindowsAuth\Plugins\ForDeployment\ %Config% || GOTO Error0
Source\CreatePackage\bin\%Config%\CreatePackage.exe SimpleWindowsAuth || GOTO Error0
RD /S /Q SimpleWindowsAuth\Plugins\ForDeployment\

IF NOT EXIST AspNetFormsAuth\Plugins\ForDeployment\ MD AspNetFormsAuth\Plugins\ForDeployment\
DEL /F /S /Q AspNetFormsAuth\Plugins\ForDeployment\*  || GOTO Error0
CALL AspNetFormsAuth\CopyPlugins.bat AspNetFormsAuth\Plugins\ForDeployment\ %Config% || GOTO Error0
Source\CreatePackage\bin\%Config%\CreatePackage.exe AspNetFormsAuth || GOTO Error0
RD /S /Q AspNetFormsAuth\Plugins\ForDeployment\

MOVE *.zip Install\ || GOTO Error0

MD Install\Rhetos
MD Install\Rhetos\bin

XCOPY /Y/D/R Source\Rhetos\bin\*.dll Install\Rhetos\bin || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\bin\*.pdb Install\Rhetos\bin || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\bin\*.exe Install\Rhetos\bin || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\bin\*.config Install\Rhetos\bin || GOTO Error0
DEL /F /Q Install\Rhetos\bin\ConnectionStrings.config

XCOPY /Y/D/R Source\Rhetos\*.aspx Install\Rhetos\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\*.asax Install\Rhetos\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\Web.config Install\Rhetos\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\*.linq Install\Rhetos\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\*.svc Install\Rhetos\ || GOTO Error0

XCOPY /Y/D/R ChangeLog.md Install\Rhetos\ || GOTO Error0
XCOPY /Y/D/R Readme.md Install\Rhetos\ || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
