FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Quepasa.Web.Client/Quepasa.Web.Client.csproj", "Quepasa.Web.Client/"]
RUN dotnet restore "Quepasa.Web.Client/Quepasa.Web.Client.csproj"
COPY . .
WORKDIR "/src/Quepasa.Web.Client"
RUN dotnet build "Quepasa.Web.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Quepasa.Web.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Quepasa.Web.Client.dll"]
