:: Build use the Premake Visual studio solution

@echo off

echo Building release...

::"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe " ..\Solution\vs2012\KEngine.Solution.sln /build Release

cmd /c %~dp0Tools\nant.bat -buildfile:%~dp0default.build

echo Build Success - Release

@ping -n 5 127.1 >nul 2>nul