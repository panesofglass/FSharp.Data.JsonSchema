dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-restore --no-build --framework net5.0
dotnet pack -c Release -o bin --no-restore --no-build
