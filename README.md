# QrFinder 🎬

Sistema de análise de QR Codes em vídeos com processamento distribuído, arquitetura de microserviços e mensageria assíncrona com Kafka.

## 📋 Hackathon - Entregáveis

Este projeto atende aos requisitos do hackathon:

✅ **Arquitetura:** Microserviços distribuídos  
✅ **Infraestrutura:** Docker Compose + Kafka + Escalabilidade horizontal  
✅ **CI/CD:** GitHub Actions + Deploy automatizado  
✅ **Funcionalidades:** Upload + Status + Resultados completos  
✅ **Documentação:** Arquitetura técnica detalhada  

📊 **[Ver Documentação Técnica](./ARCHITECTURE.md)**

## 🎯 Demonstração Rápida

### Fluxos Principais
1. **Upload de Vídeo:** `POST /video/upload-link/generate`
2. **Consulta Status:** `GET /video/{id}/status`  
3. **Consulta Resultados:** `GET /video/{id}/results`
4. **Interface Web:** http://localhost/app

## 🚀 Início Rápido

### Pré-requisitos
- Docker e Docker Compose
- Make (opcional)

### Subir o ambiente

**Com Make:**
```bash
make demo-basic
```

**Sem Make (Docker Compose apenas):**

*Ambiente Básico:*
```bash
# Subir todos os serviços
docker-compose up -d

# Verificar se todos os serviços estão rodando
docker-compose ps
```

*Ambiente Escalado (5 APIs, 10 Workers):*
```bash
# Subir ambiente escalado
docker-compose up -d --scale webapi=5 --scale analysis-worker=10

# Verificar se todos os serviços estão rodando
docker-compose ps
```

### Acessar a aplicação

- **Interface Web**: http://localhost/app/
- **API Swagger**: http://localhost/swagger/index.html
- **Kafka UI**: http://localhost:5004
- **MongoDB Express**: http://localhost:5005

## 🔥 Teste de Carga

O projeto inclui ferramentas para testar a escalabilidade do sistema com upload massivo de vídeos.

### Teste Básico vs Escalado

**1. Teste com ambiente básico (1 worker):**
```bash
# 1. Subir ambiente básico
make demo-basic

# 2. (Em terminal separado) Iniciar monitoramento
make monitor

# 3. (Em outro terminal) Executar teste de carga
make load-test
```

**2. Teste com ambiente escalado (3 workers):**
```bash
# 1. Subir ambiente escalado
make demo-scaled

# 2. (Em terminal separado) Iniciar monitoramento
make monitor

# 3. (Em outro terminal) Executar teste de carga
make load-test
```

Compare os resultados para ver a diferença de performance entre 1 worker e 3 workers processando em paralelo!

### Teste Personalizado
```bash
# Exemplo: 500 vídeos em 3 minutos
make load-test-custom VIDEOS=500 DURATION=3

# Exemplo: 100 vídeos em 1 minuto
make load-test-custom VIDEOS=100 DURATION=1
```

### Parâmetros do Teste de Carga

**Script JavaScript (`scripts/load-test.js`)**:
- `--videos=N`: Número de vídeos (padrão: 10)
- `--duration=N`: Duração em minutos (padrão: 1)
- `--url=URL`: URL base da API (padrão: http://localhost/app)

**Exemplo de execução direta**:
```bash
node scripts/load-test.js --videos=20 --duration=2
```

### Monitoramento

O script `scripts/monitor.sh` mostra em tempo real:
- 💻 **Recursos dos containers** (CPU, RAM, I/O)
- 📊 **Métricas do Kafka** (mensagens por tópico, lag dos consumers)
- 🗄️ **Estatísticas do MongoDB** (documentos por coleção)
- ⚡ **Carga do sistema** (CPU total, memória)

```bash
make monitor
```

⚠️ **Cuidado**: Paralelismo interno pode causar deadlocks em cargas altas. Prefira escalar horizontalmente (mais containers) em vez de verticalmente (mais threads por container).

## 📱 Como Usar

1. Acesse http://localhost/app/
2. Arraste um vídeo ou clique para selecionar
3. Clique em "Fazer Upload e Analisar"
4. Acompanhe o progresso em tempo real
5. Veja os QR codes encontrados nos resultados

## ⚙️ Comandos Úteis

### Com Make

```bash
# Ambiente básico (1 API, 1 Worker)
make demo-basic

# Ambiente escalado (5 APIs, 10 Workers)
make demo-scaled

# Parar tudo
make stop

# Limpar volumes
make clean
```

### Sem Make

```bash
# Subir ambiente básico
docker-compose up -d

# Ver logs
docker-compose logs -f

# Escalar workers e APIs
docker-compose up -d --scale webapi=5 --scale analysis-worker=10

# Parar todos os serviços
docker-compose down

# Limpar volumes e dados
docker-compose down -v
docker system prune -f
```

## 🏗️ Arquitetura

- **WebApp**: Interface web para upload de vídeos
- **WebAPI**: API REST para gerenciamento de vídeos
- **AnalysisWorker**: Workers que processam os vídeos
- **ResultsWorker**: Workers que processam resultados
- **NotificationsWorker**: Workers de notificações SignalR
- **SignalRServer**: Servidor de notificações em tempo real
- **Kafka**: Message broker para comunicação assíncrona
- **MongoDB**: Banco de dados para armazenar metadados
- **Azurite**: Emulador do Azure Storage para vídeos
- **Nginx**: Proxy reverso e load balancer
