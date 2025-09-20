FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Fiap.QrFinder/Fiap.QrFinder.csproj", "Fiap.QrFinder/"]
RUN dotnet restore "Fiap.QrFinder/Fiap.QrFinder.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/Fiap.QrFinder"

# Build the application
RUN dotnet build "Fiap.QrFinder.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Fiap.QrFinder.csproj" -c Release -o /app/publish

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install FFmpeg and required libs
RUN apt-get update && \
    apt-get install -y ffmpeg libgdiplus libc6-dev curl && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create a custom ffmpeg.config file that FFMpegCore might look for
RUN echo '{"BinaryFolder": "/usr/bin", "TemporaryFilesFolder": "/tmp"}' > /app/ffmpeg.config && \
    chmod +x /usr/bin/ffmpeg && \
    chmod +x /usr/bin/ffprobe

# Create uploads directory
RUN mkdir -p /app/uploads && chmod 777 /app/uploads

# Copy published app
COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Fiap.QrFinder.dll"]