REM Delete all "bin", "obj" and "TestResults" subfolders:
@FOR /F "delims=" %%i IN ('dir bin /s/b/ad') DO DEL /F/S/Q "%%i" && RD /S/Q "%%i"
@FOR /F "delims=" %%i IN ('dir obj /s/b/ad') DO DEL /F/S/Q "%%i" && RD /S/Q "%%i"
@FOR /F "delims=" %%i IN ('dir TestResult? /s/b/ad') DO DEL /F/S/Q "%%i" && RD /S/Q "%%i"

REM Delete build log:
IF EXIST msbuild.log DEL msbuild.log

REM Delete build installation result:
IF EXIST Install DEL /F/S/Q Install\*.*
IF NOT EXIST Install MD Install
