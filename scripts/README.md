# Scripts de Upload de Vídeo

Scripts para automatizar o upload e análise de vídeos no QrFinder.

## 🚀 Uso Rápido

```bash
# Via Makefile (recomendado)
make upload VIDEO=meu_video.mp4
make upload-fixed

# Script direto
./simple_upload.sh meu_video.mp4
```

## 📋 APIs Disponíveis

- `POST /video/upload-link/generate` - Gera link de upload
- `PATCH /video/{id}/analyze` - Envia para análise  
- `GET /video/{id}/status` - Status da análise
- `GET /video/{id}/results` - Resultados dos QR codes

## 📖 Documentação Completa

Ver [DEMO.md](../DEMO.md) para guia completo de uso e exemplos.