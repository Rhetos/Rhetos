REM Legacy: Saving connection string from old location.
IF EXIST "Source\Rhetos\bin\ConnectionStrings.config" IF NOT EXIST "Source\Rhetos\ConnectionStrings.config" MOVE "Source\Rhetos\bin\ConnectionStrings.config" "Source\Rhetos\ConnectionStrings.config"

REM Delete all "bin", "obj" and "TestResults" subfolders:
@FOR /F "delims=" %%i IN ('dir bin /s/b/ad') DO DEL /F/S/Q "%%i" && RD /S/Q "%%i"
@FOR /F "delims=" %%i IN ('dir obj /s/b/ad') DO DEL /F/S/Q "%%i" && RD /S/Q "%%i"
@FOR /F "delims=" %%i IN ('dir TestResult? /s/b/ad') DO DEL /F/S/Q "%%i" && RD /S/Q "%%i"

REM Delete packages' bineries and their copy in Rhetos folder:
@DEL /F/Q "*.zip"
@DEL /F/S/Q "Source\Rhetos\DataMigration"
@DEL /F/S/Q "Source\Rhetos\DslScripts"
@DEL /F/S/Q "Source\Rhetos\GeneratedFilesCache"
@DEL /F/S/Q "Source\Rhetos\PackagesCache"
@DEL /F/S/Q "Source\Rhetos\Resources"
@RD /S/Q "Source\Rhetos\DataMigration"
@RD /S/Q "Source\Rhetos\DslScripts"
@RD /S/Q "Source\Rhetos\GeneratedFilesCache"
@RD /S/Q "Source\Rhetos\PackagesCache"
@RD /S/Q "Source\Rhetos\Resources"

REM Delete build logs:
@DEL *.log

REM Delete build installation resut:
@RD /S/Q Install
@MD Install

REM Delete external dependencies cache (downloaded NuGet packages):
@RD /S/Q packages
