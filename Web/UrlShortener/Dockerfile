﻿FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /src
COPY ./Web/UrlShortener /src/UrlShortener
COPY ./Web/Data /src/Data
COPY ./Web/Entities /src/Entities

RUN dotnet build UrlShortener/Web.UrlShortener.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish UrlShortener/Web.UrlShortener.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "TerritoryTools.Web.UrlShortener.dll"]

