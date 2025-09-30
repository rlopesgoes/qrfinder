#!/bin/bash

# Script simples para upload de vídeo - apenas curl e jq necessários
# Usage: ./simple_upload.sh <video_path>

VIDEO_PATH="$1"
API_BASE="http://localhost"

if [ -z "$VIDEO_PATH" ]; then
    echo "Uso: ./simple_upload.sh <video_path>"
    exit 1
fi

echo "1. Gerando link de upload..."
RESPONSE=$(curl -s -X POST "$API_BASE/video/upload-link/generate" -H "Content-Type: application/json" -d '{}')
VIDEO_ID=$(echo "$RESPONSE" | grep -o '"videoId":"[^"]*"' | cut -d'"' -f4)
UPLOAD_URL=$(echo "$RESPONSE" | grep -o '"url":"[^"]*"' | cut -d'"' -f4)

echo "Video ID: $VIDEO_ID"

echo "2. Fazendo upload..."
curl -s -X PUT "$UPLOAD_URL" -H "x-ms-blob-type: BlockBlob" -H "Content-Type: video/mp4" --data-binary "@$VIDEO_PATH"

echo "3. Enviando para análise..."
curl -s -X PATCH "$API_BASE/video/$VIDEO_ID/analyze"

echo ""
echo "Upload concluído! Video ID: $VIDEO_ID"
echo "Verificar status: curl $API_BASE/video/$VIDEO_ID/status"
echo "Ver resultados: curl $API_BASE/video/$VIDEO_ID/results"