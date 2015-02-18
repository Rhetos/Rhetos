@IF [%1] == [] ECHO COMPOSITE VERSION NUMBER SHOULD BE GIVEN AS AN ARGUMENT OF THIS BATCH FILE. FOR EXAMPLE: ChangeRhetosServerVersion.bat 2.00.0015 & EXIT /b 1
@IF NOT [%2] == [] ECHO COMPOSITE VERSION NUMBER SHOULD BE GIVEN AS AN ARGUMENT OF THIS BATCH FILE. FOR EXAMPLE: ChangeRhetosServerVersion.bat 2.00.0015 & EXIT /b 1

FOR /F "skip=1 tokens=1-3" %%A IN ('WMIC Path Win32_LocalTime Get Day^,Month^,Year /Format:table') DO IF NOT "%%~C"=="" SET YEAR=%%~C

PUSHD .\Source\ || GOTO ERROREXIT
REM "%~dp0" is this script's folder.
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe AssemblyInfo.cs "^(\[assembly: Assembly(File|Informational)?Version(Attribute)?\(\").+(\"\)\])$" "${1}%1${4}" || GOTO ERROREXIT
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe AssemblyInfo.cs "^(\[assembly: AssemblyCompany\(\")(.*)(\"\)\])$" "${1}Omega software${3}" || GOTO ERROREXIT
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe AssemblyInfo.cs "^(\[assembly: AssemblyCopyright\(\")(.*)(\b\d{4}\b)(\"\)\])$" "${1}Copyright (C) Omega software %YEAR%${4}" || GOTO ERROREXIT
POPD

@ECHO.
@ECHO Done.
@ECHO PLEASE CHECKIN THESE FILES TO SOURCE REPOSITORY.
@EXIT /b 0

:ERROREXIT

@ECHO.
@ECHO ERROR!
@EXIT /b 1
