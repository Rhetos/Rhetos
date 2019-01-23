IF NOT EXIST Install\ MD Install
DEL /F /S /Q Install\* || GOTO Error0
IF EXIST Install\*.zip ECHO ERROR: Cannot delete old files in Install folder. Check if the files are locked. && GOTO Error0
RD /S /Q Install\RhetosServer
IF EXIST Install\RhetosServer ECHO ERROR: Cannot delete old files in Install folder. Check if the files are locked. && GOTO Error0

SET Config=%1%
IF [%1] == [] SET Config=Debug

CALL Source\Rhetos\GetServerFiles.bat %Config% /NOPAUSE || GOTO Error0

REM Packing the files with an older version of nuget.exe for backward compatibility (spaces in file names, https://github.com/Rhetos/Rhetos/issues/80).
IF NOT EXIST Install\NuGet.exe POWERSHELL (New-Object System.Net.WebClient).DownloadFile('https://dist.nuget.org/win-x86-commandline/v4.5.1/nuget.exe', 'Install\NuGet.exe') || GOTO Error0

Install\NuGet.exe pack Rhetos.nuspec -OutputDirectory Install || GOTO Error0
Install\NuGet.exe pack CommonConcepts\Rhetos.CommonConcepts.nuspec -OutputDirectory Install || GOTO Error0

MD Install\RhetosServer
MD Install\RhetosServer\bin

XCOPY /Y/D/R Source\Rhetos\bin\*.dll Install\RhetosServer\bin || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\bin\*.pdb Install\RhetosServer\bin || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\bin\Rhetos*.xml Install\RhetosServer\bin || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\bin\*.exe Install\RhetosServer\bin || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\bin\*.config Install\RhetosServer\bin || GOTO Error0
DEL /F /Q Install\RhetosServer\bin\ConnectionStrings.config

XCOPY /Y/D/R Source\Rhetos\*.aspx Install\RhetosServer\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\*.asax Install\RhetosServer\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\Web.config Install\RhetosServer\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\Template.RhetosPackages.config Install\RhetosServer\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\Template.RhetosPackageSources.config Install\RhetosServer\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\*.linq Install\RhetosServer\ || GOTO Error0
XCOPY /Y/D/R Source\Rhetos\*.svc Install\RhetosServer\ || GOTO Error0

XCOPY /Y/D/R ChangeLog.md Install\RhetosServer\ || GOTO Error0
XCOPY /Y/D/R Readme.md Install\RhetosServer\ || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
