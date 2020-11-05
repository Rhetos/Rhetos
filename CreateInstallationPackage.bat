IF NOT EXIST Install\ MD Install
DEL /F /S /Q Install\* || GOTO Error0
IF EXIST Install\*.zip ECHO ERROR: Cannot delete old files in Install folder. Check if the files are locked. && GOTO Error0
RD /S /Q Install\RhetosServer
IF EXIST Install\RhetosServer ECHO ERROR: Cannot delete old files in Install folder. Check if the files are locked. && GOTO Error0

SET Config=%1%
IF [%1] == [] SET Config=Debug

REM Packing the files with an older version of nuget.exe for backward compatibility (spaces in file names, https://github.com/Rhetos/Rhetos/issues/80).
IF NOT EXIST Install\NuGet.exe POWERSHELL (New-Object System.Net.WebClient).DownloadFile('https://dist.nuget.org/win-x86-commandline/v4.5.1/nuget.exe', 'Install\NuGet.exe') || GOTO Error0

NuGet.exe pack Source\Rhetos.nuspec -OutputDirectory Install || GOTO Error0
NuGet.exe pack Source\Rhetos.MSBuild.nuspec -OutputDirectory Install || GOTO Error0
NuGet.exe pack Source\Rhetos.Wcf.nuspec -OutputDirectory Install || GOTO Error0
NuGet.exe pack Source\Rhetos.TestCommon.nuspec -OutputDirectory Install || GOTO Error0
Install\NuGet.exe pack CommonConcepts\Rhetos.CommonConcepts.nuspec -OutputDirectory Install || GOTO Error0

MD Install\RhetosServer
MD Install\RhetosServer\bin

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
