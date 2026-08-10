FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./

COPY src/FirearmStudio.Domain/FirearmStudio.Domain.csproj                 src/FirearmStudio.Domain/
COPY src/FirearmStudio.Application/FirearmStudio.Application.csproj         src/FirearmStudio.Application/
COPY src/FirearmStudio.Infrastructure/FirearmStudio.Infrastructure.csproj  src/FirearmStudio.Infrastructure/
COPY src/FirearmStudio.WebApi/FirearmStudio.WebApi.csproj                  src/FirearmStudio.WebApi/

RUN dotnet restore src/FirearmStudio.WebApi/FirearmStudio.WebApi.csproj

COPY . .

RUN dotnet publish src/FirearmStudio.WebApi/FirearmStudio.WebApi.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /usr/share/zoneinfo /usr/share/zoneinfo
COPY --from=build /app/publish .

EXPOSE 5146
ENV ASPNETCORE_HTTP_PORTS=5146 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0

USER $APP_UID
ENTRYPOINT ["dotnet", "FirearmStudio.WebApi.dll"]
