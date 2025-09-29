# QrFinder Project Makefile

COMPOSE = docker-compose
SCRIPTS_DIR = scripts

# Cores para output
GREEN = \033[0;32m
YELLOW = \033[1;33m
RED = \033[0;31m
NC = \033[0m

.PHONY: help up down build logs clean upload upload-fixed test-upload status results scale-workers scale-analysis

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

# Development helpers
dev: ## Ambiente de desenvolvimento (up + logs)
	@make up
	@sleep 5
	@make logs

restart: ## Restart todos os serviços
	@echo "$(YELLOW)🔄 Reiniciando serviços...$(NC)"
	@make down
	@make up

restart-worker: ## Restart específico worker (uso: make restart-worker WORKER=analysis)
ifndef WORKER
	@echo "$(RED)❌ Erro: Especifique o WORKER$(NC)"
	@echo "$(YELLOW)📌 Workers: analysis, notifications, results$(NC)"
	@exit 1
endif
	@echo "$(YELLOW)🔄 Reiniciando $(WORKER)-worker...$(NC)"
	$(COMPOSE) restart $(WORKER)-worker

scale-workers: ## Escala workers (uso: make scale-workers ANALYSIS=3 RESULTS=2 NOTIFICATIONS=2)
	@echo "$(GREEN)📈 Escalando workers...$(NC)"
	@SCALE_ANALYSIS=$${ANALYSIS:-1}; \
	SCALE_RESULTS=$${RESULTS:-1}; \
	SCALE_NOTIFICATIONS=$${NOTIFICATIONS:-1}; \
	echo "Analysis: $$SCALE_ANALYSIS, Results: $$SCALE_RESULTS, Notifications: $$SCALE_NOTIFICATIONS"; \
	$(COMPOSE) up -d --scale analysis-worker=$$SCALE_ANALYSIS --scale results-worker=$$SCALE_RESULTS --scale notifications-worker=$$SCALE_NOTIFICATIONS

scale-analysis: ## Escala apenas analysis worker (uso: make scale-analysis COUNT=3)
ifndef COUNT
	@echo "$(RED)❌ Erro: Especifique o COUNT$(NC)"
	@echo "$(YELLOW)📌 Exemplo: make scale-analysis COUNT=3$(NC)"
	@exit 1
endif
	@echo "$(GREEN)📈 Escalando analysis worker para $(COUNT) instâncias...$(NC)"
	$(COMPOSE) up -d --scale analysis-worker=$(COUNT)

# Quick actions
quick-test: ## Teste rápido completo
	@echo "$(GREEN)🚀 Teste rápido completo...$(NC)"
	@make up
	@sleep 10
	@make test-upload

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
	@echo "$(YELLOW)2. Upload de vídeo:$(NC)"
	@echo "   make upload VIDEO=meu_video.mp4"
	@echo ""
	@echo "$(YELLOW)3. Upload vídeo fixo:$(NC)"
	@echo "   make upload-fixed"
	@echo ""
	@echo "$(YELLOW)4. Verificar logs:$(NC)"
	@echo "   make logs-analysis"
	@echo ""
	@echo "$(YELLOW)5. Ver resultados:$(NC)"
	@echo "   make results VIDEO_ID=uuid-do-video"