..\..\CreatePackage\bin\Debug\CreatePackage.exe TestPackage1
..\..\CreatePackage\bin\Debug\CreatePackage.exe TestPackage2
..\..\CreatePackage\bin\Debug\CreatePackage.exe TestPackage3
..\..\CreatePackage\bin\Debug\CreatePackage.exe TestPackage4
del Packages\*.zip
move *.zip Packages\
