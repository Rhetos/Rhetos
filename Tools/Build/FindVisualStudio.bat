@REM Starts Visual Studio Developer Command Prompt.
WHERE /Q MSBuild.exe && EXIT /B 0

@REM https://developercommunity.visualstudio.com/content/problem/26780/vsdevcmdbat-changes-the-current-working-directory.html
SET "VSCMD_START_DIR=%CD%"

@REM VS2017 uses vswhere.exe for locating the installation.
FOR /f "usebackq delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -prerelease -latest -property installationPath`) do (
  IF EXIST "%%i\Common7\Tools\VsDevCmd.bat" (CALL "%%i\Common7\Tools\VsDevCmd.bat" && GOTO OkRestoreEcho || GOTO Error)
)

@REM VS2015 uses %VS140COMNTOOLS% for locating the installation.
IF "%VS140COMNTOOLS%" NEQ "" (
  IF EXIST "%VS140COMNTOOLS%VsDevCmd.bat" (
    CALL "%VS140COMNTOOLS%VsDevCmd.bat" x86 && GOTO OkRestoreEcho || GOTO Error
  )
)

:Error
@ECHO ERROR: Cannot find Visual Studio.
@EXIT /B 1
:OkRestoreEcho
@ECHO ON
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
