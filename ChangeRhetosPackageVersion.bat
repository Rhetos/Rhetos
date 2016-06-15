@SETLOCAL
@IF [%2] == [] ECHO USAGE: %~nx0 SourceFolderPath VersionNumber [PrereleaseVersion]. EXAMPLE: %~nx0 "CommonConcepts" 2.0.0.15 beta.1 & EXIT /b 1
@IF NOT [%4] == [] ECHO ERROR: MORE THEN 3 ARGUMENTS ARE PROVIDED. USAGE EXAMPLE: %~nx0 "CommonConcepts" 2.0.0.15 & EXIT /b 1

FOR /F "skip=1 tokens=1-3" %%A IN ('WMIC Path Win32_LocalTime Get Day^,Month^,Year /Format:table') DO IF NOT "%%~C"=="" SET YEAR=%%~C

PUSHD %1 || GOTO Error0

SET SemVer=%2
IF NOT [%~3] == [] SET SemVer=%2-%3

REM Set simple version number:
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe *AssemblyInfo.cs "^(\[assembly: Assembly(File)?Version(Attribute)?\(\").+(\"\)\])$" "${1}%2${4}" || GOTO Error1
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe PackageInfo.xml "^(\s*\<Version\>).+(\<\/Version\>)$" "${1}%2${2}" || GOTO Error1

REM Set version number with prerelease:
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe *AssemblyInfo.cs "^(\[assembly: AssemblyInformationalVersion(Attribute)?\(\").+(\"\)\])$" "${1}%SemVer%${3}" || GOTO Error1
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe *.nuspec "^(\s*\<version\>).+(\<\/version\>\s*)$" "${1}%SemVer%${2}" || GOTO Error1

REM Set other metadata:
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe *AssemblyInfo.cs "^(\[assembly: AssemblyCompany\(\")(.*)(\"\)\])$" "${1}Omega software${3}" || GOTO Error1
"%~dp0"Source\ReplaceRegEx\bin\Debug\ReplaceRegEx.exe *AssemblyInfo.cs "^(\[assembly: AssemblyCopyright\(\")(.*)(\b\d{4}\b)(\"\)\])$" "${1}Copyright (C) Omega software %YEAR%${4}" || GOTO Error1

POPD
@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@ECHO PLEASE COMMIT THESE FILES TO SOURCE REPOSITORY.
@EXIT /B 0
:Error1
@POPD
:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
