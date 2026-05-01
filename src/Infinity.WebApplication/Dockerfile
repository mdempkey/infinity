FROM ghcr.io/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM ghcr.io/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Infinity.WebApplication/Infinity.WebApplication.csproj", "Infinity.WebApplication/"]
RUN dotnet restore "Infinity.WebApplication/Infinity.WebApplication.csproj"
COPY . .
WORKDIR "/src/Infinity.WebApplication"
RUN dotnet build "./Infinity.WebApplication.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Infinity.WebApplication.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Infinity.WebApplication.dll"]