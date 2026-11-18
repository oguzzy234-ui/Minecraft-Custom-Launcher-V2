@echo off
cd /d "%~dp0"
set DOTNET_ROOT=C:\araclar\dotnet
set PATH=C:\araclar\dotnet;%PATH%
set NUGET_PACKAGES=C:\araclar\nuget
set DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet run
pause