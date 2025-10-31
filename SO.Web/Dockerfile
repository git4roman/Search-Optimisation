FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SO.Web/SO.Web.csproj", "SO.Web/"]
COPY ["SO.Core/SO.Core.csproj", "SO.Core/"]
COPY ["SO.Data/SO.Data.csproj", "SO.Data/"]
RUN dotnet restore "SO.Web/SO.Web.csproj"
COPY . .
WORKDIR "/src/SO.Web"
RUN dotnet build "./SO.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SO.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SO.Web.dll"]
