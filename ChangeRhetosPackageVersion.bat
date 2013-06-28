@IF [%2] == [] ECHO THE PACKAGE'S PATH AND COMPOSITE VERSION NUMBER SHOULD BE GIVEN AS AN ARGUMENT OF THIS BATCH FILE. FOR EXAMPLE: ChangeRhetosPackageVersion.bat CommonConcepts 2.0.0.15 & EXIT /b 1
@IF NOT [%3] == [] ECHO THE PACKAGE'S PATH AND COMPOSITE VERSION NUMBER SHOULD BE GIVEN AS AN ARGUMENT OF THIS BATCH FILE. FOR EXAMPLE: ChangeRhetosPackageVersion.bat CommonConcepts 2.0.0.15 & EXIT /b 1

SET ThisScriptFolder=%~dp0
FOR /F "skip=1 tokens=1-3" %%A IN ('WMIC Path Win32_LocalTime Get Day^,Month^,Year /Format:table') DO IF NOT "%%~C"=="" SET YEAR=%%~C

PUSHD %1 || GOTO ERROREXIT
"%ThisScriptFolder%External\Omega\ReplaceRegEx.exe" AssemblyInfo.cs "^(\[assembly: Assembly(File|Informational)?Version(Attribute)?\(\").+(\"\)\])$" "${1}%2${4}" || GOTO ERROREXIT
"%ThisScriptFolder%External\Omega\ReplaceRegEx.exe" AssemblyInfo.cs "^(\[assembly: AssemblyCompany\(\")(.*)(\"\)\])$" "${1}Omega software${3}" || GOTO ERROREXIT
"%ThisScriptFolder%External\Omega\ReplaceRegEx.exe" AssemblyInfo.cs "^(\[assembly: AssemblyCopyright\(\")(.*)(\b\d{4}\b)(\"\)\])$" "${1}Copyright (c) Omega software %YEAR%${4}" || GOTO ERROREXIT
"%ThisScriptFolder%External\Omega\ReplaceRegEx.exe" PackageInfo.xml "^(  \<Version\>).+(\<\/Version\>)$" "${1}%2${2}" || GOTO ERROREXIT
POPD

@ECHO.
@ECHO Done.
@ECHO PLEASE CHECKIN THESE FILES TO SOURCE REPOSITORY.
@EXIT /b 0

:ERROREXIT

@ECHO.
@ECHO ERROR!
@EXIT /b 1
