FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Instala apenas librdkafka para Kafka
RUN apt-get update && apt-get install -y \
    librdkafka-dev \
    librdkafka1 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ResultsWorker/ResultsWorker.csproj", "src/ResultsWorker/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Contracts/Contracts.csproj", "src/Contracts/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
RUN dotnet restore "src/ResultsWorker/ResultsWorker.csproj"
COPY . .
WORKDIR "/src/src/ResultsWorker"
RUN dotnet build "ResultsWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResultsWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configura usuário não-root para segurança
RUN groupadd -g 1001 appgroup && \
    useradd -r -u 1001 -g appgroup appuser && \
    chown -R appuser:appgroup /app

USER appuser

ENTRYPOINT ["dotnet", "ResultsWorker.dll"]