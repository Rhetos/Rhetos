CALL ChangeRhetosServerVersion.bat 0.9.6.0 || EXIT /B 1
CALL ChangeRhetosPackageVersion.bat CommonConcepts 0.9.6.0 || EXIT /B 1
CALL ChangeRhetosPackageVersion.bat MvcModelGenerator 0.9.6.0 || EXIT /B 1
@REM CALL ChangeRhetosPackageVersion.bat ..\SomeOtherPackage 1.3.0.0 || EXIT /B 1
