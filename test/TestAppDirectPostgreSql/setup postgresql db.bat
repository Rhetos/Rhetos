SETLOCAL
SET IMAGE=postgres:16.2
SET CONTAINER_NAME=rhetos_test_postgres
SET DB_USER=postgres
SET DB_NAME=rhetos6testappdirect
SET DB_NAME2=rhetos6commonconceptstestapp
SET HOST=localhost
SET PORT=5432

docker version 1> nul || (echo PLEASE INSTALL AND RUN DOCKER DESKTOP. & PAUSE)
docker pull %IMAGE% || PAUSE
docker run --name %CONTAINER_NAME% -e POSTGRES_HOST_AUTH_METHOD=trust -d -p %PORT%:5432 %IMAGE% || PAUSE

@echo Waiting for PostgreSQL to start...
@set /a retries=0
:TestContainerStarted
@if %retries% GEQ 3 echo PostgreSQL did not start in time. & exit /b 1
docker exec %CONTAINER_NAME% psql -U %DB_USER% -h %HOST% -p %PORT% -c "\q" && GOTO PgIsReady
timeout /t 1 >NUL
@set /a retries=%retries% + 1
goto TestContainerStarted
:PgIsReady
@echo PostgreSQL is ready.

@echo Creating new database "%DB_NAME%"
docker exec %CONTAINER_NAME% psql -U %DB_USER% -h %HOST% -p %PORT% -c "CREATE DATABASE %DB_NAME%;" || PAUSE
@echo Creating new database "%DB_NAME2%"
docker exec %CONTAINER_NAME% psql -U %DB_USER% -h %HOST% -p %PORT% -c "CREATE DATABASE %DB_NAME2%;" || PAUSE

@echo Checking connection to the database...
docker exec %CONTAINER_NAME% psql -U %DB_USER% -h %HOST% -p %PORT% -d %DB_NAME% -c "\conninfo" || PAUSE
@echo Checking connection to the database...
docker exec %CONTAINER_NAME% psql -U %DB_USER% -h %HOST% -p %PORT% -d %DB_NAME2% -c "\conninfo" || PAUSE

@rem @echo Stopping and removing container "%CONTAINER_NAME%"...
@rem docker stop %CONTAINER_NAME% || PAUSE
@rem docker rm %CONTAINER_NAME% || PAUSE

@echo Done.
