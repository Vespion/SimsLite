FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Back/SimServer/SimServer.csproj", "SimServer/"]
RUN dotnet restore "Back/SimServer/SimServer.csproj"
COPY . .
WORKDIR "/src/SimServer"
RUN dotnet build "SimServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SimServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimServer.dll"]
