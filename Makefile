.PHONY: help infra-up infra-down infra-logs infra-ps full-up full-down clean build-api build-worker run-api run-worker

help:
	@echo "QRFinder - Comandos Disponíveis:"
	@echo ""
	@echo "🏗️  INFRAESTRUTURA:"
	@echo "  make infra-up        - Sobe apenas a infraestrutura"
	@echo "  make infra-down      - Para a infraestrutura"
	@echo "  make infra-logs      - Mostra logs da infraestrutura"
	@echo "  make infra-ps        - Status dos containers"
	@echo ""
	@echo "🚀  APLICAÇÃO COMPLETA:"
	@echo "  make full-up         - Sobe tudo (infra + API + Worker)"
	@echo "  make full-down       - Para tudo"
	@echo ""
	@echo "💻  DESENVOLVIMENTO LOCAL:"
	@echo "  make build-api       - Builda a API"
	@echo "  make build-worker    - Builda o Worker"
	@echo "  make run-api         - Roda API localmente"
	@echo "  make run-worker      - Roda Worker localmente"
	@echo ""
	@echo "🧹  LIMPEZA:"
	@echo "  make clean           - Remove containers e volumes"

infra-up:
	@echo "🏗️  Subindo infraestrutura..."
	docker-compose -f docker-compose.infra.yml up -d
	@echo "✅ Infraestrutura rodando!"

infra-down:
	@echo "🛑 Parando infraestrutura..."
	docker-compose -f docker-compose.infra.yml down

infra-logs:
	docker-compose -f docker-compose.infra.yml logs -f

infra-ps:
	docker-compose -f docker-compose.infra.yml ps

full-up:
	@echo "🚀 Subindo aplicação completa..."
	docker-compose up -d
	@echo "✅ Aplicação completa rodando!"

full-down:
	@echo "🛑 Parando aplicação completa..."
	docker-compose down

build-api:
	@echo "🔨 Buildando API..."
	cd src/WebApi && dotnet build

build-worker:
	@echo "🔨 Buildando Worker..."
	cd src/Worker && dotnet build

run-api:
	@echo "💻 Rodando API localmente na porta 5000..."
	@echo "📋 Acesse: http://localhost:5000/swagger"
	cd src/WebApi && dotnet run --urls="http://localhost:5000"

run-worker:
	@echo "💻 Rodando Worker localmente..."
	cd src/Worker && dotnet run

clean:
	@echo "🧹 Limpando containers, volumes e imagens..."
	docker-compose down -v --remove-orphans
	docker-compose -f docker-compose.infra.yml down -v --remove-orphans
	docker system prune -f
	@echo "✅ Limpeza concluída!"
