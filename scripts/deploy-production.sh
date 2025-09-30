#!/bin/bash

# Deploy Script for Digital Ocean VPS
# Usage: ./scripts/deploy-production.sh

set -e

echo "🚀 Starting production deployment..."

# Check if required environment variables are set
if [ -z "$AZURE_STORAGE_CONNECTION_STRING" ]; then
    echo "❌ Error: AZURE_STORAGE_CONNECTION_STRING is not set"
    exit 1
fi

if [ -z "$GITHUB_REPOSITORY" ]; then
    echo "❌ Error: GITHUB_REPOSITORY is not set (format: username/repo)"
    exit 1
fi

# Create app directory if it doesn't exist
sudo mkdir -p /opt/qrfinder
cd /opt/qrfinder

# Copy production files if they don't exist
if [ ! -f docker-compose.prod.yml ]; then
    echo "📋 Copying docker-compose.prod.yml..."
    sudo cp /tmp/qrfinder/docker-compose.prod.yml .
fi

if [ ! -f infra/nginx.conf ]; then
    echo "📋 Copying nginx configuration..."
    sudo mkdir -p infra
    sudo cp /tmp/qrfinder/infra/nginx.conf infra/
fi

# Create .env file from environment variables
echo "🔧 Creating .env file..."
sudo tee .env > /dev/null <<EOF
GITHUB_REPOSITORY=${GITHUB_REPOSITORY}
MONGO_PASSWORD=${MONGO_PASSWORD:-$(openssl rand -base64 32)}
AZURE_STORAGE_CONNECTION_STRING=${AZURE_STORAGE_CONNECTION_STRING}
AZURE_STORAGE_CONTAINER_NAME=${AZURE_STORAGE_CONTAINER_NAME:-videos}
EOF

# Pull latest images
echo "📦 Pulling latest Docker images..."
docker-compose -f docker-compose.prod.yml pull

# Stop current services
echo "⏹️ Stopping current services..."
docker-compose -f docker-compose.prod.yml down || true

# Start services
echo "▶️ Starting services..."
docker-compose -f docker-compose.prod.yml up -d

# Wait for services to be healthy
echo "🏥 Waiting for services to be healthy..."
sleep 30

# Check service status
echo "📊 Service status:"
docker-compose -f docker-compose.prod.yml ps

# Clean up old images
echo "🧹 Cleaning up old images..."
docker image prune -f

echo "✅ Deployment completed successfully!"
echo "🌐 Application should be available at: http://$(curl -s ifconfig.me)"
echo "📋 Check logs with: docker-compose -f docker-compose.prod.yml logs -f"