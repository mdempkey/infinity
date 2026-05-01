FROM ghcr.io/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM ghcr.io/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Infinity.WebApi/Infinity.WebApi.csproj", "Infinity.WebApi/"]
RUN dotnet restore "Infinity.WebApi/Infinity.WebApi.csproj"
COPY . .
WORKDIR "/src/Infinity.WebApi"
RUN dotnet build "./Infinity.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Infinity.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Infinity.WebApi.dll"]
