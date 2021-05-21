SETLOCAL

@SET Config=%1%
@IF [%1] == [] SET Config=Debug

IF NOT EXIST Install\ MD Install
DEL /F /S /Q Install\* || GOTO Error0

NuGet.exe pack Source\Rhetos.nuspec -OutputDirectory Install -Exclude "**.pdb" || GOTO Error0
NuGet.exe pack Source\Rhetos.Host.Net.nuspec -OutputDirectory Install -Exclude "**.pdb" || GOTO Error0
NuGet.exe pack Source\Rhetos.Host.AspNet.nuspec -OutputDirectory Install -Exclude "**.pdb" || GOTO Error0
NuGet.exe pack Source\Rhetos.MSBuild.nuspec -OutputDirectory Install -Exclude "**.pdb" || GOTO Error0
NuGet.exe pack Source\Rhetos.Host.nuspec -OutputDirectory Install -Exclude "**.pdb" || GOTO Error0
NuGet.exe pack Source\Rhetos.TestCommon.nuspec -OutputDirectory Install -Exclude "**.pdb" || GOTO Error0
NuGet.exe pack CommonConcepts\Rhetos.CommonConcepts.nuspec -OutputDirectory Install -Exclude "**.pdb" || GOTO Error0

NuGet.exe pack Source\Rhetos.nuspec  -Symbols -SymbolPackageFormat snupkg -OutputDirectory Install || GOTO Error0
NuGet.exe pack Source\Rhetos.Host.Net.nuspec -Symbols -SymbolPackageFormat snupkg -OutputDirectory Install || GOTO Error0
NuGet.exe pack Source\Rhetos.Host.AspNet.nuspec -Symbols -SymbolPackageFormat snupkg -OutputDirectory Install || GOTO Error0
NuGet.exe pack Source\Rhetos.MSBuild.nuspec -Symbols -SymbolPackageFormat snupkg -OutputDirectory Install || GOTO Error0
NuGet.exe pack Source\Rhetos.Host.nuspec -Symbols -SymbolPackageFormat snupkg -OutputDirectory Install || GOTO Error0
NuGet.exe pack Source\Rhetos.TestCommon.nuspec -Symbols -SymbolPackageFormat snupkg -OutputDirectory Install || GOTO Error0
NuGet.exe pack CommonConcepts\Rhetos.CommonConcepts.nuspec -Symbols -SymbolPackageFormat snupkg -OutputDirectory Install || GOTO Error0

@REM ================================================

@ECHO.
@ECHO %~nx0 SUCCESSFULLY COMPLETED.
@EXIT /B 0

:Error0
@ECHO.
@ECHO %~nx0 FAILED.
@EXIT /B 1
