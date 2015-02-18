@SETLOCAL
@SET BuildVersion=0.9.24.1
CALL ChangeRhetosServerVersion.bat %BuildVersion% || EXIT /B 1
CALL ChangeRhetosPackageVersion.bat CommonConcepts %BuildVersion% || EXIT /B 1
CALL ChangeRhetosPackageVersion.bat CommonConcepts\CommonConceptsTest %BuildVersion% || EXIT /B 1
CALL ChangeRhetosPackageVersion.bat SimpleWindowsAuth %BuildVersion% || EXIT /B 1
CALL ChangeRhetosPackageVersion.bat AspNetFormsAuth %BuildVersion% || EXIT /B 1
