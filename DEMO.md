# 🎬 QrFinder - Guia de Demonstrações

Este guia apresenta demonstrações pré-configuradas do sistema QrFinder para diferentes cenários de uso.

## 🚀 Demonstrações Disponíveis

### 1. Demo Básico (Desenvolvimento)
Ambiente simples com 1 instância de cada serviço.

```bash
make demo-basic
```

**Configuração:**
- 1 WebAPI
- 1 Analysis Worker  
- 1 Results Worker
- 1 Notifications Worker
- 1 SignalR Server

**Ideal para:**
- Desenvolvimento local
- Testes básicos
- Debug de funcionalidades

---

### 2. Demo Escalado (Produção)
Ambiente de alta performance com múltiplas instâncias.

```bash
make demo-scaled
```

**Configuração:**
- 5 WebAPIs (load balanced via Nginx)
- 10 Analysis Workers (processamento paralelo)
- 1 Results Worker
- 1 Notifications Worker
- 1 SignalR Server

**Ideal para:**
- Testes de performance
- Simulação de ambiente de produção
- Processamento de múltiplos vídeos simultaneamente

---

## 📊 Serviços de Monitoramento

Ambos os ambientes incluem:

| Serviço | URL | Descrição |
|---------|-----|-----------|
| **API Principal** | http://localhost | Upload e consulta de vídeos |
| **Swagger** | http://localhost/swagger/index.html | Documentação da API |
| **Kafka UI** | http://localhost:5004 | Monitoramento de filas |
| **Mongo Express** | http://localhost:5005 | Database admin (admin/admin123) |

---

## 🎯 Testes Automatizados

### Teste Rápido
```bash
make quick-test
```
- Sobe ambiente básico
- Faz upload de vídeo de teste
- Exibe logs de processamento

### Teste de Performance
```bash
make performance-test
```
- Sobe ambiente escalado (10 workers)
- Faz upload de vídeo
- Demonstra processamento paralelo

---

## ⚙️ Escala Manual

### Escalar Serviços Específicos
```bash
# Escalar apenas Analysis Workers
make scale-all ANALYSIS=15

# Escalar múltiplos serviços
make scale-all API=3 ANALYSIS=8 RESULTS=2 NOTIFICATIONS=2
```

### Verificar Status
```bash
# Ver containers rodando
docker-compose ps

# Ver logs de workers
make logs-analysis

# Monitorar saúde dos serviços
make health
```

---

## 📈 Capacidade de Processamento

### Demo Básico
- **Throughput:** 1 vídeo por vez
- **Latência:** Baixa (sem concorrência)
- **Recursos:** Mínimos

### Demo Escalado
- **Throughput:** 10 vídeos simultâneos
- **Latência:** Distribuída entre workers
- **Recursos:** Alto processamento paralelo

### Kafka Configuration
- **Partições:** 10 (permite até 10 consumers paralelos)
- **Replication Factor:** 1
- **Auto-commit:** Desabilitado (controle manual)

---

## 🔧 Comandos Úteis

```bash
# Ver ajuda completa
make help

# Parar todos os serviços
make down

# Rebuild completo
make clean
make build

# Upload de vídeo personalizado
make upload VIDEO=meu_video.mp4

# Ver resultados de processamento
make results VIDEO_ID=uuid-do-video

# Ver status de processamento
make status VIDEO_ID=uuid-do-video
```

---

## 🐛 Troubleshooting

### Verificar Logs
```bash
# Logs de todos os serviços
make logs

# Logs específicos
make logs-analysis
make logs-results
make logs-notifications
```

### Reiniciar Serviços
```bash
# Restart completo
make restart

# Restart de worker específico
make restart-worker WORKER=analysis
```

### Verificar Saúde
```bash
# Health check de todos os serviços
make health

# Verificar Kafka topics
docker exec qrfinder-kafka kafka-topics --bootstrap-server kafka:29092 --list
```

---

## 🎪 Exemplo de Uso Completo

```bash
# 1. Subir ambiente escalado
make demo-scaled

# 2. Aguardar inicialização (15 segundos)
sleep 15

# 3. Fazer upload de vídeo
make upload-fixed

# 4. Monitorar processamento no Kafka UI
open http://localhost:5004

# 5. Ver logs em tempo real
make logs-analysis

# 6. Verificar resultados
# (Use o VIDEO_ID retornado no upload)
make results VIDEO_ID=<uuid>
```

Com esta configuração, você pode processar até **10 vídeos simultaneamente** com rastreamento completo de performance e logs centralizados.