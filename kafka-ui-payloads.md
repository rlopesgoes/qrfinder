# Payloads para Kafka UI

Acesse: http://localhost:8080
- Topic: `video-notifications`
- Partition: 0 (ou qualquer)

## 1. UPLOADING (📤)
```json
{
  "VideoId": "test-video-123",
  "Stage": 0,
  "ProgressPercentage": 25.5,
  "CurrentOperation": "Receiving video chunks",
  "ErrorMessage": null,
  "Timestamp": "2025-01-20T10:30:00Z"
}
```

## 2. UPLOADED (✅)
```json
{
  "VideoId": "test-video-123",
  "Stage": 1,
  "ProgressPercentage": 100.0,
  "CurrentOperation": "Upload completed successfully",
  "ErrorMessage": null,
  "Timestamp": "2025-01-20T10:32:15Z"
}
```

## 3. PROCESSING - Início (⚙️)
```json
{
  "VideoId": "test-video-123",
  "Stage": 2,
  "ProgressPercentage": 10.0,
  "CurrentOperation": "Initializing video decoder",
  "ErrorMessage": null,
  "Timestamp": "2025-01-20T10:33:00Z"
}
```

## 4. PROCESSING - Meio (⚙️)
```json
{
  "VideoId": "test-video-123",
  "Stage": 2,
  "ProgressPercentage": 60.0,
  "CurrentOperation": "Extracting QR codes from frames",
  "ErrorMessage": null,
  "Timestamp": "2025-01-20T10:35:30Z"
}
```

## 5. PROCESSING - Quase fim (⚙️)
```json
{
  "VideoId": "test-video-123",
  "Stage": 2,
  "ProgressPercentage": 90.0,
  "CurrentOperation": "Finalizing QR code analysis",
  "ErrorMessage": null,
  "Timestamp": "2025-01-20T10:39:00Z"
}
```

## 6. PROCESSED (🎉)
```json
{
  "VideoId": "test-video-123",
  "Stage": 3,
  "ProgressPercentage": 100.0,
  "CurrentOperation": "Found 5 QR codes successfully",
  "ErrorMessage": null,
  "Timestamp": "2025-01-20T10:40:45Z"
}
```

## 7. FAILED - Upload (❌)
```json
{
  "VideoId": "failed-video-upload",
  "Stage": 4,
  "ProgressPercentage": 15.0,
  "CurrentOperation": "Upload failed",
  "ErrorMessage": "Network timeout during chunk upload",
  "Timestamp": "2025-01-20T10:25:10Z"
}
```

## 8. FAILED - Processing (❌)
```json
{
  "VideoId": "failed-video-process",
  "Stage": 4,
  "ProgressPercentage": 45.0,
  "CurrentOperation": "Processing failed",
  "ErrorMessage": "Invalid video format: unsupported codec H.265",
  "Timestamp": "2025-01-20T10:28:30Z"
}
```

## 9. FAILED - Corrupted (❌)
```json
{
  "VideoId": "corrupted-video",
  "Stage": 4,
  "ProgressPercentage": 80.0,
  "CurrentOperation": "Frame extraction failed",
  "ErrorMessage": "Video file is corrupted or incomplete",
  "Timestamp": "2025-01-20T10:35:50Z"
}
```

## 10. Teste com vídeo diferente - UPLOADING
```json
{
  "VideoId": "demo-video-456",
  "Stage": 0,
  "ProgressPercentage": 75.0,
  "CurrentOperation": "Uploading final chunks",
  "ErrorMessage": null,
  "Timestamp": "2025-01-20T11:15:20Z"
}
```

---

## Como usar no Kafka UI:

1. Acesse http://localhost:8080
2. Vá em **Topics** → **video-notifications**
3. Clique em **Produce Message**
4. Cole um dos JSONs acima no campo **Value**
5. Clique **Produce**
6. Verifique no cliente SignalR se recebeu a mensagem

## Para testar sequência completa:
Execute os payloads 1 → 2 → 3 → 4 → 5 → 6 em ordem, com alguns segundos de intervalo entre cada um.

## Mapeamento de Stages:
- `0` = UPLOADING
- `1` = UPLOADED  
- `2` = PROCESSING
- `3` = PROCESSED
- `4` = FAILED