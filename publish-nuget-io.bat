set /P version=Enter version for Asv.IO: 
cd src\Asv.IO\bin\Release\
dotnet nuget push Asv.IO.%version%.nupkg --source https://api.nuget.org/v3/index.json
dotnet nuget push Asv.IO.%version%.nupkg --source https://nuget.pkg.github.com/asvol/index.json