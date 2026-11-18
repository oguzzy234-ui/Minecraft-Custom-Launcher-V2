@echo off
cd /d "%~dp0"
set DOTNET_ROOT=C:\araclar\dotnet
set PATH=C:\araclar\dotnet;%PATH%
set NUGET_PACKAGES=C:\araclar\nuget
set DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet publish Baslatici.csproj -c Release -r win-x64 --self-contained true -o C:\araclar\BaslaticiExe
echo.
echo Bitti. Launcher: C:\araclar\BaslaticiExe\Baslatici.exe
pause