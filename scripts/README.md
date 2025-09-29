# Scripts de Upload de Vídeo

Scripts para automatizar o upload e análise de vídeos no QrFinder.

## Pré-requisitos

- Docker Compose rodando: `docker-compose up -d`
- `curl` instalado (já vem no macOS/Linux)

## Opção 1: Script Simples (Recomendado)

Apenas usa `curl` - funciona em qualquer sistema:

```bash
./simple_upload.sh meu_video.mp4
```

## Opção 2: Script Completo

Requer `jq` para formatação JSON:

```bash
# Instalar jq (se necessário)
brew install jq  # macOS
apt install jq   # Linux

# Usar o script
./upload_video.sh meu_video.mp4
```

## Opção 3: Python (Para quem já tem Python)

```bash
python upload_video.py meu_video.mp4
```

## Manual via curl

Se preferir fazer manualmente:

```bash
# 1. Gerar link de upload
curl -X POST http://localhost/video/upload-link/generate \
  -H "Content-Type: application/json" -d '{}'

# 2. Upload (usar o uploadUrl retornado)
curl -X PUT "UPLOAD_URL_AQUI" \
  -H "x-ms-blob-type: BlockBlob" \
  -H "Content-Type: video/mp4" \
  --data-binary "@meu_video.mp4"

# 3. Enfileirar para análise (usar o videoId retornado)
curl -X PATCH http://localhost/video/VIDEO_ID_AQUI/analyze

# 4. Verificar status
curl http://localhost/video/VIDEO_ID_AQUI/status

# 5. Ver resultados (quando completo)
curl http://localhost/video/VIDEO_ID_AQUI/results
```

## Exemplo de Uso

```bash
# Com um vídeo de teste
./simple_upload.sh example.mp4

# Saída esperada:
# 1. Gerando link de upload...
# Video ID: 12345678-1234-1234-1234-123456789abc
# 2. Fazendo upload...
# 3. Enviando para análise...
# 
# Upload concluído! Video ID: 12345678-1234-1234-1234-123456789abc
# Verificar status: curl http://localhost/video/12345678-1234-1234-1234-123456789abc/status
# Ver resultados: curl http://localhost/video/12345678-1234-1234-1234-123456789abc/results
```

## Monitoramento em Tempo Real

Para acompanhar o progresso:

```bash
# Verificar status a cada 5 segundos
watch -n 5 "curl -s http://localhost/video/VIDEO_ID_AQUI/status | jq ."

# Ver logs dos workers
docker logs -f qrfinder-analysis-worker
```

## APIs Disponíveis

- `POST /video/upload-link/generate` - Gera link de upload
- `PATCH /video/{id}/analyze` - Envia para análise  
- `GET /video/{id}/status` - Status da análise
- `GET /video/{id}/results` - Resultados dos QR codes