# QrFinder Project Makefile

COMPOSE = docker-compose
SCRIPTS_DIR = scripts

# Cores para output
GREEN = \033[0;32m
YELLOW = \033[1;33m
RED = \033[0;31m
NC = \033[0m

.PHONY: help up down build logs clean upload upload-fixed test-upload status results demo-basic demo-scaled scale-all

help: ## Mostra esta ajuda
	@echo "$(GREEN)QrFinder - Comandos Disponíveis:$(NC)"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "$(YELLOW)%-20s$(NC) %s\n", $$1, $$2}'

# Docker Commands
up: ## Sobe todos os serviços
	@echo "$(GREEN)🚀 Subindo todos os serviços...$(NC)"
	$(COMPOSE) up -d --remove-orphans

down: ## Para todos os serviços
	@echo "$(RED)⏹️ Parando todos os serviços...$(NC)"
	$(COMPOSE) down

build: ## Rebuild todos os containers
	@echo "$(YELLOW)🔨 Fazendo rebuild dos containers...$(NC)"
	$(COMPOSE) build

logs: ## Mostra logs de todos os serviços
	@echo "$(GREEN)📋 Logs dos serviços...$(NC)"
	$(COMPOSE) logs -f

clean: ## Remove containers, imagens e volumes
	@echo "$(RED)🧹 Limpando containers, imagens e volumes...$(NC)"
	$(COMPOSE) down -v --rmi all --remove-orphans

# Worker-specific commands
logs-analysis: ## Logs do Analysis Worker
	$(COMPOSE) logs -f analysis-worker

logs-notifications: ## Logs do Notifications Worker
	$(COMPOSE) logs -f notifications-worker

logs-results: ## Logs do Results Worker
	$(COMPOSE) logs -f results-worker

logs-signalr: ## Logs do SignalR Server
	$(COMPOSE) logs -f signalr-server

# Video Upload Commands
upload: ## Upload de vídeo (uso: make upload VIDEO=meu_video.mp4)
ifndef VIDEO
	@echo "$(RED)❌ Erro: Especifique o vídeo com VIDEO=arquivo$(NC)"
	@echo "$(YELLOW)📌 Exemplo: make upload VIDEO=example.mp4$(NC)"
	@exit 1
endif
	@echo "$(GREEN)📤 Fazendo upload do vídeo: $(VIDEO)$(NC)"
	@chmod +x $(SCRIPTS_DIR)/simple_upload.sh
	@$(SCRIPTS_DIR)/simple_upload.sh $(VIDEO)

upload-full: ## Upload com interface completa (uso: make upload-full VIDEO=meu_video.mp4)
ifndef VIDEO
	@echo "$(RED)❌ Erro: Especifique o vídeo com VIDEO=arquivo$(NC)"
	@echo "$(YELLOW)📌 Exemplo: make upload-full VIDEO=example.mp4$(NC)"
	@exit 1
endif
	@echo "$(GREEN)📤 Fazendo upload completo do vídeo: $(VIDEO)$(NC)"
	@chmod +x $(SCRIPTS_DIR)/upload_video.sh
	@$(SCRIPTS_DIR)/upload_video.sh $(VIDEO)

test-upload: ## Faz upload de um vídeo de teste
	@echo "$(YELLOW)🎬 Testando upload...$(NC)"
	@if [ ! -f example.mp4 ]; then echo "$(RED)❌ Coloque um arquivo 'example.mp4' na raiz do projeto$(NC)"; exit 1; fi
	@make upload VIDEO=example.mp4

# API Commands
status: ## Verifica status de um vídeo (uso: make status VIDEO_ID=uuid)
ifndef VIDEO_ID
	@echo "$(RED)❌ Erro: Especifique o VIDEO_ID$(NC)"
	@echo "$(YELLOW)📌 Exemplo: make status VIDEO_ID=12345678-1234-1234-1234-123456789abc$(NC)"
	@exit 1
endif
	@echo "$(GREEN)📊 Status do vídeo $(VIDEO_ID):$(NC)"
	@curl -s http://localhost/video/$(VIDEO_ID)/status | jq . || echo "$(RED)❌ Erro ao buscar status$(NC)"

results: ## Mostra resultados de um vídeo (uso: make results VIDEO_ID=uuid)
ifndef VIDEO_ID
	@echo "$(RED)❌ Erro: Especifique o VIDEO_ID$(NC)"
	@echo "$(YELLOW)📌 Exemplo: make results VIDEO_ID=12345678-1234-1234-1234-123456789abc$(NC)"
	@exit 1
endif
	@echo "$(GREEN)📋 Resultados do vídeo $(VIDEO_ID):$(NC)"
	@curl -s http://localhost/video/$(VIDEO_ID)/results | jq . || echo "$(RED)❌ Erro ao buscar resultados$(NC)"

# Health checks
health: ## Verifica saúde dos serviços
	@echo "$(GREEN)🏥 Verificando saúde dos serviços...$(NC)"
	@echo "WebAPI:"
	@curl -s http://localhost/health 2>/dev/null || echo "$(RED)❌ WebAPI não responde$(NC)"
	@echo "\nSignalR Server:"
	@curl -s http://localhost:5010/health 2>/dev/null || echo "$(RED)❌ SignalR não responde$(NC)"
	@echo "\nContainers:"
	@$(COMPOSE) ps

# Ambientes pré-configurados
demo-basic: ## 🚀 Demo básico: 1 instância de cada serviço
	@echo "$(GREEN)🚀 Subindo ambiente básico (1 réplica de cada)...$(NC)"
	@$(COMPOSE) down
	@$(COMPOSE) up -d
	@echo "$(GREEN)✅ Ambiente básico rodando!$(NC)"
	@echo "$(YELLOW)📊 Serviços disponíveis:$(NC)"
	@echo "  • 🎬 WebApp (Upload UI): http://localhost/app"
	@echo "  • 🔧 API: http://localhost"
	@echo "  • 📋 Swagger: http://localhost/swagger/index.html"
	@echo "  • 📊 Kafka UI: http://localhost:5004"
	@echo "  • 🗄️ Mongo Express: http://localhost:5005 (admin/admin123)"

demo-scaled: ## 🔥 Demo escalado: 5 APIs + 10 Analysis Workers + 1 Results + 1 Notifications
	@echo "$(GREEN)🔥 Subindo ambiente escalado...$(NC)"
	@echo "$(YELLOW)📈 Configuração: 5 APIs, 10 Analysis, 1 Results, 1 Notifications$(NC)"
	@$(COMPOSE) down
	@$(COMPOSE) up -d --scale webapi=5 --scale analysis-worker=10 --scale results-worker=1 --scale notifications-worker=1
	@echo "$(GREEN)✅ Ambiente escalado rodando!$(NC)"
	@echo "$(YELLOW)🎬 WebApp (Upload UI): http://localhost/app$(NC)"
	@echo "$(YELLOW)📊 Load balancer (Nginx): http://localhost$(NC)"
	@echo "$(YELLOW)📋 Swagger: http://localhost/swagger/index.html$(NC)"
	@echo "$(YELLOW)⚡ 10 workers processando em paralelo$(NC)"

# Comandos de escala manual
scale-all: ## Escala todos os serviços (uso: make scale-all API=3 ANALYSIS=5 RESULTS=2 NOTIFICATIONS=2)
	@echo "$(GREEN)📈 Escalando todos os serviços...$(NC)"
	@API_COUNT=$${API:-1}; \
	ANALYSIS_COUNT=$${ANALYSIS:-1}; \
	RESULTS_COUNT=$${RESULTS:-1}; \
	NOTIFICATIONS_COUNT=$${NOTIFICATIONS:-1}; \
	echo "API: $$API_COUNT, Analysis: $$ANALYSIS_COUNT, Results: $$RESULTS_COUNT, Notifications: $$NOTIFICATIONS_COUNT"; \
	$(COMPOSE) up -d --scale webapi=$$API_COUNT --scale analysis-worker=$$ANALYSIS_COUNT --scale results-worker=$$RESULTS_COUNT --scale notifications-worker=$$NOTIFICATIONS_COUNT

# Comandos de desenvolvimento
dev: ## Ambiente de desenvolvimento (up + logs)
	@make demo-basic
	@sleep 5
	@make logs

restart: ## Restart todos os serviços
	@echo "$(YELLOW)🔄 Reiniciando serviços...$(NC)"
	@make down
	@make demo-basic

# Quick actions
quick-test: ## Teste rápido completo
	@echo "$(GREEN)🚀 Teste rápido completo...$(NC)"
	@make demo-basic
	@sleep 10
	@make test-upload

performance-test: ## Teste de performance com ambiente escalado
	@echo "$(GREEN)🔥 Teste de performance com 10 workers...$(NC)"
	@make demo-scaled
	@sleep 15
	@echo "$(YELLOW)🎬 Fazendo upload para testar processamento paralelo...$(NC)"
	@make upload-fixed

upload-fixed: ## Upload do vídeo fixo WhatsApp
	@echo "$(GREEN)📤 Fazendo upload do vídeo fixo WhatsApp...$(NC)"
	@chmod +x $(SCRIPTS_DIR)/simple_upload.sh
	@$(SCRIPTS_DIR)/simple_upload.sh "/Users/renatojsilva-dev/Downloads/WhatsApp Video 2025-09-21 at 17.47.53.mp4"

# Exemplos
examples: ## Mostra exemplos de uso
	@echo "$(GREEN)📚 Exemplos de Uso:$(NC)"
	@echo ""
	@echo "$(YELLOW)1. Subir ambiente:$(NC)"
	@echo "   make up"
	@echo ""
	@echo "$(YELLOW)2. Demo básico (1 réplica):$(NC)"
	@echo "   make demo-basic"
	@echo ""
	@echo "$(YELLOW)3. Demo escalado (10 workers):$(NC)"
	@echo "   make demo-scaled"
	@echo ""
	@echo "$(YELLOW)4. Upload de vídeo:$(NC)"
	@echo "   make upload VIDEO=meu_video.mp4"
	@echo ""
	@echo "$(YELLOW)5. Teste de performance:$(NC)"
	@echo "   make performance-test"