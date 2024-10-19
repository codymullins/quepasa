FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Add Node.js installation stage
FROM node:20-alpine AS node-build
WORKDIR /src
COPY ["package.json", "/"]
RUN npm install @tailwindcss/cli@4.0.0-alpha.28
COPY . .
WORKDIR "/src"
RUN npm run tailwind

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY --from=node-build /src/Quepasa.Web/wwwroot/app.css /src/Quepasa.Web/wwwroot/app.css
COPY ["Quepasa.Web/Quepasa.Web.csproj", "Quepasa.Web/"]
COPY ["Quepasa.Web.Client/Quepasa.Web.Client.csproj", "Quepasa.Web.Client/"]
RUN dotnet restore "Quepasa.Web/Quepasa.Web.csproj"
COPY . .
WORKDIR "/src/Quepasa.Web"
RUN dotnet build "Quepasa.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Quepasa.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Quepasa.Web.dll"]
