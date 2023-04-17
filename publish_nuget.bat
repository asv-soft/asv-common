@echo off
rem ====== projects ======

set projects=Asv.Cfg Asv.Common Asv.IO Asv.Store

rem ====== projects ======

rem copy version to text file, then in variable
git describe --abbrev=0 >./version.txt
SET /p VERSION=<version.txt
DEL version.txt

(for %%p in (%projects%) do (
	cd src\%%p\bin\Release\
	dotnet nuget push %%p.%VERSION:~1%.nupkg --source https://api.nuget.org/v3/index.json
	dotnet nuget push %%p.%VERSION:~1%.nupkg --source https://nuget.pkg.github.com/asvol/index.json
	cd ../../../../
)) 





