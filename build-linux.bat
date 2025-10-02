@echo off
title Building Poop ^& PoopAPI
rd ".build/gamedata" /S /Q
rd ".build/modules" /S /Q
rd ".build/shared" /S /Q
cls
dotnet publish PoopAPI/PoopAPI.csproj -f net9.0 -r linux-x64 --disable-build-servers --no-self-contained -c Release --output ".build/shared/Poop.API"
dotnet publish Poop/Poop.csproj -f net9.0 -r linux-x64 --disable-build-servers --no-self-contained -c Release --output ".build/modules/Poop"
echo:
echo Build Poop ^& PoopAPI Completed...
echo:
echo Renaming appsettings.json to appsettings.example.json...
if exist ".build\modules\Poop\appsettings.json" move ".build\modules\Poop\appsettings.json" ".build\modules\Poop\appsettings.example.json"
echo:
echo Copying GameData...
xcopy "gamedata\*" ".build/gamedata/" /E /I /Y
echo:
echo Copy Configs Completed...
echo:
pause