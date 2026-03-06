FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/StorageLens.Services.Scanner/StorageLens.Services.Scanner.csproj
RUN dotnet publish src/StorageLens.Services.Scanner/StorageLens.Services.Scanner.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StorageLens.Services.Scanner.dll"]
