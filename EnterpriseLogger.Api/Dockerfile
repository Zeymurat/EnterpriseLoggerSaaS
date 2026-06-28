FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EnterpriseLogger.sln ./
COPY EnterpriseLogger.Domain/EnterpriseLogger.Domain.csproj EnterpriseLogger.Domain/
COPY EnterpriseLogger.Application/EnterpriseLogger.Application.csproj EnterpriseLogger.Application/
COPY EnterpriseLogger.Infrastructure/EnterpriseLogger.Infrastructure.csproj EnterpriseLogger.Infrastructure/
COPY EnterpriseLogger.Api/EnterpriseLogger.Api.csproj EnterpriseLogger.Api/

RUN dotnet restore EnterpriseLogger.Api/EnterpriseLogger.Api.csproj

COPY EnterpriseLogger.Domain/ EnterpriseLogger.Domain/
COPY EnterpriseLogger.Application/ EnterpriseLogger.Application/
COPY EnterpriseLogger.Infrastructure/ EnterpriseLogger.Infrastructure/
COPY EnterpriseLogger.Api/ EnterpriseLogger.Api/

RUN dotnet publish EnterpriseLogger.Api/EnterpriseLogger.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EnterpriseLogger.Api.dll"]
