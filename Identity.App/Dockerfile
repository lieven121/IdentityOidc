# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Identity.App/Identity.App.csproj", "Identity.App/"]
RUN dotnet restore "./Identity.App/Identity.App.csproj"
COPY . .
WORKDIR "/src/Identity.App"
RUN dotnet build "./Identity.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM node:22-alpine AS node-build
WORKDIR /src
COPY ["Identity.Client/package.json", "Identity.Client/package-lock.json", "./"]
RUN npm install
COPY Identity.Client .
RUN rm -rf ./dist
RUN npm run build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Identity.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=node-build /src/dist ./wwwroot
ENTRYPOINT ["dotnet", "Identity.App.dll"]