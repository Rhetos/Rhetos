@SETLOCAL
SET Config=%1%
IF [%1] == [] SET Config=Debug

REM Set current working folder to folder where this script is, to ensure that the relative paths in this script are valid.
PUSHD "%~dp0"

XCOPY /Y/D/R ..\..\Source\CreateIISExpressSite\bin\%Config%\CreateIISExpressSite.??? bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CreateIISExpressSite\bin\%Config%\CreateIISExpressSite.exe.config bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CreateIISExpressSite\bin\%Config%\Template.IISExpress.config bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CreateIISExpressSite\bin\%Config%\*.dll bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CreateIISExpressSite\bin\%Config%\*.pdb bin\ || GOTO Error1

XCOPY /Y/D/R ..\..\Source\CreateAndSetDatabase\bin\%Config%\CreateAndSetDatabase.??? bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CreateAndSetDatabase\bin\%Config%\CreateAndSetDatabase.exe.config bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CreateAndSetDatabase\bin\%Config%\*.dll bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CreateAndSetDatabase\bin\%Config%\*.pdb bin\ || GOTO Error1

XCOPY /Y/D/R ..\..\Source\DeployPackages\bin\%Config%\DeployPackages.??? bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\DeployPackages\bin\%Config%\DeployPackages.exe.config bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\DeployPackages\bin\%Config%\*.dll bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\DeployPackages\bin\%Config%\*.pdb bin\ || GOTO Error1

XCOPY /Y/D/R ..\..\Source\CleanupOldData\bin\%Config%\CleanupOldData.??? bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CleanupOldData\bin\%Config%\CleanupOldData.exe.config bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CleanupOldData\bin\%Config%\*.dll bin\ || GOTO Error1
XCOPY /Y/D/R ..\..\Source\CleanupOldData\bin\%Config%\*.pdb bin\ || GOTO Error1

XCOPY /Y/D/R ..\..\Source\RhetosWebApi\Template.ConnectionStrings.config bin\ || GOTO Error1

@POPD

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error1
@POPD
:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@IF /I [%2] NEQ [/NOPAUSE] @PAUSE
@EXIT /B 1
