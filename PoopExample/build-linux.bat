@echo off
title Building PoopExample
cls
echo Building PoopExample for Linux...
echo:
dotnet publish PoopExample.csproj -f net9.0 -r linux-x64 --disable-build-servers --no-self-contained -c Release --output "../.build/modules/PoopExample"
echo:
echo Build PoopExample Completed...
echo Output: ../.build/modules/PoopExample
echo:
pause
