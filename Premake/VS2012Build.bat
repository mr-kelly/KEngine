:: Build use the Premake Visual studio solution

@echo off

echo Building release...

"C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe " ..\Solution\vs2012\KEngine.Solution.sln /build Release

echo Build Success - Release

pause