@echo off

net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo Reopen in elevated shell
    exit /b 1
)
set p=artifacts\Debug\unpack_zip.msi
IF EXIST "%p%"  (
    IF EXIST "C:\Program Files\unpack_zip" (
        msiexec /x "%p%" /quiet

        IF %errorlevel% NEQ 0 (
            echo Failed to uninstall the previous version
            exit /b 1
        )
    )
)
nuke --Configuration Debug
IF %errorlevel% NEQ 0 (
    echo Failed to create program
    exit /b 1
)
msiexec /i "%p%" /quiet ACCEPT_EULA=1 IACCEPTLICENSE=YES EULA_ACCEPTED=1
IF %errorlevel% NEQ 0 (
    echo Failed to install
    exit /b 1
)
