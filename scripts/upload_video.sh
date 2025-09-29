#!/bin/bash

# Script para upload e análise de vídeos no QrFinder
# Usage: ./upload_video.sh <video_path>

set -e

VIDEO_PATH="$1"
API_BASE="http://localhost"

if [ -z "$VIDEO_PATH" ]; then
    echo "❌ Uso: ./upload_video.sh <caminho_do_video>"
    echo "📌 Exemplo: ./upload_video.sh example.mp4"
    exit 1
fi

if [ ! -f "$VIDEO_PATH" ]; then
    echo "❌ Arquivo não encontrado: $VIDEO_PATH"
    exit 1
fi

echo "🎥 Processando vídeo: $VIDEO_PATH"
echo "🌐 API: $API_BASE"
echo "$(printf '%*s' 50 '' | tr ' ' '-')"

# 1. Gerar link de upload
echo "🔗 Gerando link de upload..."
UPLOAD_RESPONSE=$(curl -s -X POST "$API_BASE/video/upload-link/generate" \
    -H "Content-Type: application/json" \
    -d '{}')

VIDEO_ID=$(echo "$UPLOAD_RESPONSE" | jq -r '.videoId')
UPLOAD_URL=$(echo "$UPLOAD_RESPONSE" | jq -r '.uploadUrl')

echo "🆔 Video ID: $VIDEO_ID"

# 2. Upload do vídeo
echo "📤 Fazendo upload do arquivo..."
curl -s -X PUT "$UPLOAD_URL" \
    -H "x-ms-blob-type: BlockBlob" \
    -H "Content-Type: video/mp4" \
    --data-binary "@$VIDEO_PATH"

echo "✅ Upload concluído!"

# 3. Enfileirar para análise
echo "🚀 Enviando vídeo para análise..."
ENQUEUE_RESPONSE=$(curl -s -X PATCH "$API_BASE/video/$VIDEO_ID/analyze")
ENQUEUED_AT=$(echo "$ENQUEUE_RESPONSE" | jq -r '.enqueuedAt')

echo "📅 Enfileirado em: $ENQUEUED_AT"

# 4. Aguardar conclusão
echo "⏳ Aguardando conclusão da análise..."
while true; do
    STATUS_RESPONSE=$(curl -s "$API_BASE/video/$VIDEO_ID/status" || echo '{"status":"ERROR"}')
    STATUS=$(echo "$STATUS_RESPONSE" | jq -r '.status // "UNKNOWN"')
    
    echo "📊 Status atual: $STATUS"
    
    if [[ "$STATUS" == "PROCESSED" || "$STATUS" == "COMPLETED" ]]; then
        echo "✅ Análise concluída!"
        break
    elif [[ "$STATUS" == "FAILED" || "$STATUS" == "ERROR" ]]; then
        echo "❌ Análise falhou!"
        echo "$STATUS_RESPONSE" | jq .
        exit 1
    fi
    
    sleep 5
done

# 5. Obter resultados
echo ""
echo "$(printf '%*s' 50 '' | tr ' ' '=')"
echo "📋 RESULTADOS DA ANÁLISE"
echo "$(printf '%*s' 50 '' | tr ' ' '=')"

RESULTS=$(curl -s "$API_BASE/video/$VIDEO_ID/results")
TOTAL_QR=$(echo "$RESULTS" | jq -r '.totalQrCodes // 0')

echo "🔍 Total de QR Codes encontrados: $TOTAL_QR"

if [ "$TOTAL_QR" -gt 0 ]; then
    echo ""
    echo "📱 QR Codes detectados:"
    echo "$(printf '%*s' 30 '' | tr ' ' '-')"
    
    echo "$RESULTS" | jq -r '.qrCodes[] | "\(.content) (⏰ \(.formattedTimestamp // .timestampSeconds))"' | \
    awk '{printf "%2d. %s\n", NR, $0}'
else
    echo "ℹ️  Nenhum QR Code foi detectado no vídeo"
fi

echo ""
echo "🔗 Para monitorar: $API_BASE/video/$VIDEO_ID/status"
echo "🔗 Para resultados: $API_BASE/video/$VIDEO_ID/results"