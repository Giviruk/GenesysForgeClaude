# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# слои восстановления кешируются отдельно от исходников
COPY nuget.config ./
COPY src/GenesysForge.Domain/GenesysForge.Domain.csproj src/GenesysForge.Domain/
COPY src/GenesysForge.Application/GenesysForge.Application.csproj src/GenesysForge.Application/
COPY src/GenesysForge.Infrastructure/GenesysForge.Infrastructure.csproj src/GenesysForge.Infrastructure/
COPY src/GenesysForge.Api/GenesysForge.Api.csproj src/GenesysForge.Api/
RUN dotnet restore src/GenesysForge.Api/GenesysForge.Api.csproj

COPY src/ src/
# Полные private-описания (embedded resource в Infrastructure.csproj: ..\..\private-content\*.ru.json).
# Должны попасть в контекст до publish, иначе PrivateFull в образе будет неполным.
# Перед публичным открытием репо удаляются только *.ru.json (каталог с README остаётся,
# иначе COPY упадёт); тогда glob не находит файлов и private-сид падает на safe-описания.
COPY private-content/ private-content/
RUN dotnet publish src/GenesysForge.Api/GenesysForge.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GenesysForge.Api.dll"]
