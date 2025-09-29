# QrFinder 🎬

Sistema de análise de QR Codes em vídeos com processamento distribuído usando Kafka.

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
# Copiar variáveis de ambiente
cp .env.example .env

# Subir todos os serviços
docker-compose up -d

# Verificar se todos os serviços estão rodando
docker-compose ps
```

*Ambiente Escalado (5 APIs, 10 Workers):*
```bash
# Copiar variáveis de ambiente
cp .env.example .env

# Subir ambiente escalado
docker-compose up -d --scale webapi=5 --scale analysis-worker=10

# Verificar se todos os serviços estão rodando
docker-compose ps
```

### Acessar a aplicação

- **Interface Web**: http://localhost/app/
- **API Swagger**: http://localhost/swagger/index.html
- **Kafka UI**: http://localhost:8082
- **MongoDB Express**: http://localhost:5005

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

## 📊 Monitoramento

- **Logs em tempo real**: `docker-compose logs -f`
- **Status dos serviços**: `docker-compose ps`
- **Kafka UI**: http://localhost:8082
- **MongoDB**: http://localhost:5005

## 🔧 Solução de Problemas

### Serviços não iniciam
```bash
# Verificar logs
docker-compose logs

# Reiniciar serviços
docker-compose restart
```

### Tópicos Kafka não encontrados
```bash
# Verificar se kafka-topics-init terminou
docker-compose ps kafka-topics-init

# Ver logs da inicialização dos tópicos
docker-compose logs kafka-topics-init
```

### Problemas de upload
```bash
# Verificar se Azurite está rodando
docker-compose ps azurite

# Verificar logs do WebAPI
docker-compose logs webapi
```

## 🛠️ Desenvolvimento

### Rebuild de serviços específicos
```bash
# Rebuild e restart do WebAPI
docker-compose build webapi && docker-compose restart webapi

# Rebuild de todos os workers
docker-compose build analysis-worker results-worker notifications-worker
docker-compose restart analysis-worker results-worker notifications-worker
```

### Executar comandos no container
```bash
# MongoDB shell
docker exec -it qrfinder-mongo mongosh

# Kafka topics
docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:9092 --list

# Logs específicos
docker-compose logs -f analysis-worker
```

## 📝 Notas

- Todos os tópicos Kafka são criados automaticamente com 10 partições
- O sistema aguarda o Kafka estar pronto antes de iniciar os workers
- Upload máximo de 500MB por vídeo
- SignalR fornece notificações em tempo real do progresso
- Backup via polling caso SignalR falhe