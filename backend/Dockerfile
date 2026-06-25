# ---- restore + shared source ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# слои восстановления кешируются отдельно от исходников
COPY nuget.config ./
COPY src/GenesysForge.Domain/GenesysForge.Domain.csproj src/GenesysForge.Domain/
COPY src/GenesysForge.Application/GenesysForge.Application.csproj src/GenesysForge.Application/
COPY src/GenesysForge.Infrastructure/GenesysForge.Infrastructure.csproj src/GenesysForge.Infrastructure/
COPY src/GenesysForge.Api/GenesysForge.Api.csproj src/GenesysForge.Api/
RUN dotnet restore src/GenesysForge.Api/GenesysForge.Api.csproj

FROM restore AS source
COPY src/ src/

# ---- private build: embeds private-content resources ----
FROM source AS private-build
# Полные private-описания (embedded resource в Infrastructure.csproj: ..\..\private-content\*.ru.json).
COPY private-content/ private-content/
RUN dotnet publish src/GenesysForge.Api/GenesysForge.Api.csproj -c Release -o /app --no-restore

# ---- public build: explicitly excludes private embedded resources ----
FROM source AS public-build
RUN dotnet publish src/GenesysForge.Api/GenesysForge.Api.csproj -c Release -o /app --no-restore \
    /p:IncludePrivateContent=false

# ---- shared runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GenesysForge.Api.dll"]

FROM runtime AS private
COPY --from=private-build /app .

FROM runtime AS public
COPY --from=public-build /app .

# Local/default builds remain PrivateFull-compatible.
FROM private AS final
