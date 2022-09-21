set /P version=Enter version for Asv.Common: 
cd src\Asv.Common\bin\Release\
dotnet nuget push Asv.Common.%version%.nupkg --source https://api.nuget.org/v3/index.json
dotnet nuget push Asv.Common.%version%.nupkg --source https://nuget.pkg.github.com/asvol/index.json