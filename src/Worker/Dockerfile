FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
WORKDIR /app

# Instala FFmpeg para processamento de vídeo 
# ImageSharp é puro .NET, não precisa de dependências nativas
RUN apk add --no-cache ffmpeg

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["src/Worker/Worker.csproj", "src/Worker/"]
COPY ["src/Application/Application/Application.csproj", "src/Application/Application/"]
COPY ["src/Contracts/Contracts.csproj", "src/Contracts/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
RUN dotnet restore "src/Worker/Worker.csproj"
COPY . .
WORKDIR "/src/src/Worker"
RUN dotnet build "Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Cria diretório de trabalho para vídeos temporários
RUN mkdir -p /tmp/qrfinder/videos

# Configura usuário não-root para segurança
RUN addgroup -g 1001 -S appgroup && \
    adduser -S appuser -u 1001 -G appgroup && \
    chown -R appuser:appgroup /app /tmp/qrfinder

USER appuser

ENTRYPOINT ["dotnet", "Worker.dll"]