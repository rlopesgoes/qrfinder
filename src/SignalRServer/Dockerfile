FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/SignalRServer/SignalRServer.csproj", "src/SignalRServer/"]
RUN dotnet restore "src/SignalRServer/SignalRServer.csproj"
COPY . .
WORKDIR "/src/src/SignalRServer"
RUN dotnet build "SignalRServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SignalRServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configura usuário não-root para segurança
RUN groupadd -g 1001 appgroup && \
    useradd -r -u 1001 -g appgroup appuser && \
    chown -R appuser:appgroup /app

USER appuser

ENTRYPOINT ["dotnet", "SignalRServer.dll"]