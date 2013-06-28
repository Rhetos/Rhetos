@ECHO OFF

SET Config=%1%
IF [%1] == [] SET Config=Debug

SET StartTime=%Time%

hg pull || echo CANNOT AUTOMATICALLY GET LATEST VERSION. IT IS POSSIBLE THAT A MANUAL MERGE IS NEEDED. && PAUSE && EXIT /B 1
hg update || echo CANNOT AUTOMATICALLY GET LATEST VERSION. IT IS POSSIBLE THAT A MANUAL MERGE IS NEEDED. && PAUSE && EXIT /B 1

ECHO.
ECHO ================================================
ECHO CLEAN
CALL Clean.bat

ECHO.
ECHO ================================================
ECHO BUILD
CALL Build.bat %Config% || GOTO ErrorBuild
FOR /F "usebackq" %%A IN ('BuildErrors.log') DO SET Size=%%~zA
IF 0%Size% GTR 0 GOTO ErrorBuild
SET Size=

ECHO.
ECHO ================================================
ECHO TEST
CALL Test.bat %Config% || GOTO ErrorBuild
FOR /F "usebackq" %%A IN ('TestErrors.log') DO SET Size=%%~zA
IF 0%Size% GTR 0 GOTO ErrorTest
SET Size=

ECHO.
ECHO ================================================
ECHO CREATE PACKAGES
CALL CreateInstallationPackage.bat %Config% || GOTO ErrorCreateInstallationPackage

ECHO.
ECHO ================================================
ECHO Start: %StartTime%
ECHO End:   %Time%
ECHO.
ECHO Build successful.
PAUSE
EXIT /b


:ErrorBuild
ECHO.
ECHO.
ECHO ================================================
ECHO !!!!!!!!!!!!!!!! ERRORS IN BUILD !!!!!!!!!!!!!!!!!!!!
TYPE BuildErrors.log
ECHO !!!!!!!!!!!!!!!! ERRORS IN BUILD !!!!!!!!!!!!!!!!!!!!
PAUSE
START .
EXIT /b


:ErrorTest
ECHO.
ECHO.
ECHO ================================================
ECHO !!!!!!!!!!!!!!!! ERRORS IN UNIT TESTING !!!!!!!!!!!!!!!!!!!!
TYPE TestErrors.log
ECHO !!!!!!!!!!!!!!!! ERRORS IN UNIT TESTING !!!!!!!!!!!!!!!!!!!!
PAUSE
START .
EXIT /b


:ErrorCreateInstallationPackage
ECHO.
ECHO.
ECHO ================================================
ECHO !!!!!!!!!!!!!!!! CreateInstallationPackage.bat FAILED !!!!!!!!!!!!!!!!!!!!
PAUSE
START .
EXIT /b
