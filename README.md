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

**2. Teste com ambiente escalado (2 workers):**
```bash
# 1. Subir ambiente escalado
make demo-scaled

# 2. (Em terminal separado) Iniciar monitoramento
make monitor

# 3. (Em outro terminal) Executar teste de carga
make load-test
```

Compare os resultados para ver a diferença de performance entre 1 worker e 2 workers processando em paralelo!

**⚠️ Solução de Problemas:**
Se os workers travarem durante o teste:
```bash
# Limpa estado e reinicia workers
make clean-state

# Ou apenas reinicia workers
make restart-workers
```

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

### Relatórios de Performance

Após o teste, são gerados:
- 📊 **Console**: Relatório detalhado com métricas
- 📄 **JSON**: `scripts/load-test-results.json` com dados completos
- 📈 **Análise**: Recomendações de otimização

**Exemplo de relatório**:
```
🎯 LOAD TEST RESULTS
==================================================
📊 Total Videos: 60
✅ Successful: 58
❌ Failed: 2
📈 Success Rate: 96.7%
⏱️  Total Duration: 1m 12s
🚀 Actual Rate: 48 videos/min

📋 Upload Times:
   Average: 845ms
   Min: 234ms
   Max: 2567ms

🟢 EXCELLENT: System handled load very well
```

### Paralelismo nos Workers (Opcional)

Por padrão, cada worker processa **1 vídeo por vez** para evitar travamentos. Para habilitar processamento paralelo dentro de cada worker:

**Configuração via variável de ambiente:**
```bash
# No docker-compose.yml, adicione:
environment:
  - ANALYSISSWORKER__MAXCONCURRENCY=3
```

**Ou via appsettings.json:**
```json
{
  "AnalysisWorker": {
    "MaxConcurrency": 3
  }
}
```

⚠️ **Cuidado**: Paralelismo interno pode causar deadlocks em cargas altas. Prefira escalar horizontalmente (mais containers) em vez de verticalmente (mais threads por container).

### Escalabilidade Recomendada

Para diferentes volumes de vídeos por minuto:

| Videos/min | WebAPI | Analysis Workers | Threads/Worker | Total Capacity |
|------------|--------|------------------|----------------|----------------|
| < 50       | 1      | 2-3              | 1              | 2-3 simultâneos |
| 50-100     | 1-2    | 5-8              | 1              | 5-8 simultâneos |
| 100-200    | 2-3    | 10-15            | 1              | 10-15 simultâneos |
| > 200      | 3+     | 15+              | 1              | 15+ simultâneos |

**Comandos para escalar**:
```bash
# Escalabilidade média (100 videos/min)
make scale-all API=2 ANALYSIS=10 RESULTS=2 NOTIFICATIONS=1

# Alta escalabilidade (200+ videos/min)
make scale-all API=3 ANALYSIS=15 RESULTS=3 NOTIFICATIONS=2
```

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