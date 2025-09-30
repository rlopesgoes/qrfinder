FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Instala FFmpeg para processamento de vídeo, librdkafka e dependências do SkiaSharp
RUN apt-get update && apt-get install -y \
    ffmpeg \
    librdkafka-dev \
    librdkafka1 \
    libfontconfig1 \
    libfreetype6 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    libc6-dev \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AnalysisWorker/AnalysisWorker.csproj", "src/AnalysisWorker/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Contracts/Contracts.csproj", "src/Contracts/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
RUN dotnet restore "src/AnalysisWorker/AnalysisWorker.csproj"
COPY . .
WORKDIR "/src/src/AnalysisWorker"
RUN dotnet build "AnalysisWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AnalysisWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Cria diretório de trabalho para vídeos temporários
RUN mkdir -p /tmp/qrfinder/videos

# Configura usuário não-root para segurança
RUN groupadd -g 1001 appgroup && \
    useradd -r -u 1001 -g appgroup appuser && \
    chown -R appuser:appgroup /app /tmp/qrfinder

USER appuser

ENTRYPOINT ["dotnet", "AnalysisWorker.dll"]