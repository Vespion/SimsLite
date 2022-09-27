FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Back/AuthServer/AuthServer.csproj", "AuthServer/"]
COPY ["Back/ServerBase/ServerBase.csproj", "ServerBase/"]
COPY ["Back/ServerBase/ServerObjects.csproj", "ServerObjects/"]
COPY ["Common/FrontDTOs/FrontDTOs.csproj", "FrontDTOs/"]
RUN dotnet restore "Back/AuthServer/AuthServer.csproj"
COPY . .
WORKDIR "/src/AuthServer"
RUN dotnet build "AuthServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
ENTRYPOINT ["dotnet", "AuthServer.dll"]
COPY --from=publish /app/publish .
