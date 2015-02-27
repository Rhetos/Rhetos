@IF [%2] == [] ECHO THE PACKAGE'S PATH AND COMPOSITE VERSION NUMBER SHOULD BE GIVEN AS AN ARGUMENT OF THIS BATCH FILE. FOR EXAMPLE: ChangeRhetosPackageVersion.bat CommonConcepts 2.0.0.15 & EXIT /b 1
@IF NOT [%3] == [] ECHO THE PACKAGE'S PATH AND COMPOSITE VERSION NUMBER SHOULD BE GIVEN AS AN ARGUMENT OF THIS BATCH FILE. FOR EXAMPLE: ChangeRhetosPackageVersion.bat CommonConcepts 2.0.0.15 & EXIT /b 1

FOR /F "skip=1 tokens=1-3" %%A IN ('WMIC Path Win32_LocalTime Get Day^,Month^,Year /Format:table') DO IF NOT "%%~C"=="" SET YEAR=%%~C

PUSHD %1 || GOTO ERROREXIT
REM "%~dp0" is this script's folder.
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe *AssemblyInfo.cs "^(\[assembly: Assembly(File|Informational)?Version(Attribute)?\(\").+(\"\)\])$" "${1}%2${4}" || GOTO ERROREXIT
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe *AssemblyInfo.cs "^(\[assembly: AssemblyCompany\(\")(.*)(\"\)\])$" "${1}Omega software${3}" || GOTO ERROREXIT
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe *AssemblyInfo.cs "^(\[assembly: AssemblyCopyright\(\")(.*)(\b\d{4}\b)(\"\)\])$" "${1}Copyright (C) Omega software %YEAR%${4}" || GOTO ERROREXIT
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe PackageInfo.xml "^(  \<Version\>).+(\<\/Version\>)$" "${1}%2${2}" || GOTO ERROREXIT
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe *.nuspec "^(\s*\<version\>).+(\<\/version\>\s*)$" "${1}%2${2}" /RootOnly || GOTO ERROREXIT
POPD

@ECHO.
@ECHO Done.
@ECHO PLEASE CHECKIN THESE FILES TO SOURCE REPOSITORY.
@EXIT /b 0

:ERROREXIT

@ECHO.
@ECHO ERROR!
@EXIT /b 1
