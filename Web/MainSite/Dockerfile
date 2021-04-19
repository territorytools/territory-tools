FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src

COPY Web/MainSite/Web.MainSite.csproj Web/MainSite/Web.MainSite.csproj
COPY Web/Data/Web.Data.csproj Web/Data/Web.Data.csproj
COPY Web/Entities/Web.Entities.csproj Web/Entities/Web.Entities.csproj
COPY Alba/Controllers/Alba.Controllers.csproj Alba/Controllers/Alba.Controllers.csproj

RUN dotnet restore Web/MainSite/Web.MainSite.csproj --packages /src/packages

COPY . .

RUN dotnet build Web/MainSite/Web.MainSite.csproj -c Release -o /app --no-restore -f netcoreapp3.1

FROM build AS publish
RUN mkdir /app/publish && mkdir /app/publish/zh-Hant/
RUN dotnet publish Web/MainSite/Web.MainSite.csproj -c Release -o /app/publish --no-restore -f netcoreapp3.1

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 80

ENTRYPOINT ["dotnet", "TerritoryTools.Web.MainSite.dll"]

