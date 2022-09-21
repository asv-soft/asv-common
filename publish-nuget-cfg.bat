set /P version=Enter version for Asv.Cfg: 
cd src\Asv.Cfg\bin\Release\
dotnet nuget push Asv.Cfg.%version%.nupkg --source https://api.nuget.org/v3/index.json
dotnet nuget push Asv.Cfg.%version%.nupkg --source https://nuget.pkg.github.com/asvol/index.json