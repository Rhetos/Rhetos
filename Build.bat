@REM HINT: SET SECOND ARGUMENT TO /NOPAUSE WHEN AUTOMATING THE BUILD.

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

@IF DEFINED VisualStudioVersion GOTO SkipVcvarsall
IF "%VS140COMNTOOLS%" NEQ "" CALL "%VS140COMNTOOLS%VsDevCmd.bat" x86 && GOTO EndVcvarsall || GOTO Error0
IF "%VS120COMNTOOLS%" NEQ "" CALL "%VS120COMNTOOLS%\..\..\VC\vcvarsall.bat" x86 && GOTO EndVcvarsall || GOTO Error0
IF "%VS110COMNTOOLS%" NEQ "" CALL "%VS110COMNTOOLS%\..\..\VC\vcvarsall.bat" x86 && GOTO EndVcvarsall || GOTO Error0
IF "%VS100COMNTOOLS%" NEQ "" CALL "%VS100COMNTOOLS%\..\..\VC\vcvarsall.bat" x86 && GOTO EndVcvarsall || GOTO Error0
ECHO ERROR: Cannot detect Visual Studio, missing VSxxxCOMNTOOLS variable.
GOTO Error0
:EndVcvarsall
@ECHO ON
:SkipVcvarsall

IF EXIST msbuild.log DEL msbuild.log || GOTO Error0

REM ReplaceRegEx.exe is a local tool used in ChangeVersions.bat.
MSBuild.exe "Source\ReplaceRegEx\ReplaceRegEx.csproj" /p:Configuration=Debug /verbosity:minimal /fileLogger || GOTO Error0

REM Updating the version of all projects to match the one written in ChangeVersions.bat file.
CALL ChangeVersions.bat /NOPAUSE || GOTO Error0

REM NuGet Automatic Package Restore requires "NuGet.exe restore" to be executed before the command-line build.
WHERE /Q NuGet.exe || ECHO ERROR: Please download the NuGet.exe command line tool. && GOTO Error0
NuGet.exe restore Rhetos.sln -NonInteractive || GOTO Error0
MSBuild.exe "Rhetos.sln" /target:rebuild /p:Configuration=%Config% /verbosity:minimal /fileLogger || GOTO Error0
CALL CreateInstallationPackage.bat %Config% /NOPAUSE || GOTO Error0

REM Updating the version of all projects back to "internal development build".
CALL ChangeVersions.bat /NOPAUSE /RESTORE || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
