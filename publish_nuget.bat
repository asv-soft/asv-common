@echo off
rem ====== projects ======

set projects=set projects=Asv.Cfg Asv.Common Asv.IO Asv.Store
set source=https://api.nuget.org/v3/index.json

rem ====== projects ======

rem set git to global PATH
SET BASH_PATH="%SYSTEMDRIVE%\Program Files\Git\bin"
SET PATH=%BASH_PATH%;%PATH%

 rem copy version to text file, then in variable
git describe --tags --abbrev=4 >>version.txt
SET /p VERSION=<version.txt
del version.txt

rem Remove first v from version
SET VERSION=%VERSION:v=%

(for %%p in (%projects%) do (
	cd src\%%p\bin\Release\
	dotnet nuget push %%p.%VERSION%.nupkg --source %source%
	cd ../../../../
)) 
pause





