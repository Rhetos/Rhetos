IF EXIST bin RD bin /S /Q
REM Allow Windows Explorer to release file locks.
IF EXIST bin RD bin /S /Q
IF EXIST bin RD bin /S /Q
IF EXIST bin ECHO ERROR: Cannot clear LOCKED folder 'bin' & PAUSE
MD bin

XCOPY /S/Y/D/R ..\DeployPackages\bin\* bin\
