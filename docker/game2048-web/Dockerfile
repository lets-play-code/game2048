FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Game2048.sln ./
COPY src/Game2048.Game/Game2048.Game.csproj src/Game2048.Game/
COPY src/Game2048.Web/Game2048.Web.csproj src/Game2048.Web/

RUN dotnet restore src/Game2048.Web/Game2048.Web.csproj --nologo

COPY src/Game2048.Game/ src/Game2048.Game/
COPY src/Game2048.Web/ src/Game2048.Web/

RUN dotnet publish src/Game2048.Web/Game2048.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 5000

ENTRYPOINT ["dotnet", "Game2048.Web.dll"]
