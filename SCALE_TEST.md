# 🚀 Teste de Escalabilidade - WebAPI + Nginx

## Pré-requisitos
```bash
# Certifique-se que o Docker está rodando
docker --version
docker-compose --version
```

## 1️⃣ Build inicial da aplicação
```bash
# Na raiz do projeto
make build
# ou
docker-compose build webapi
```

## 2️⃣ Subir apenas infraestrutura (MongoDB, Kafka)
```bash
make infra-up
# ou
docker-compose -f docker-compose.infra.yml up -d

# Verificar se está rodando
docker ps
```

## 3️⃣ Subir WebAPI (1 réplica) + Nginx
```bash
# Subir apenas WebAPI e Nginx
docker-compose up -d webapi nginx

# Verificar containers
docker ps | grep -E "(webapi|nginx)"
```

## 4️⃣ Testar acesso inicial
```bash
# Via Nginx (load balancer)
curl http://localhost/health
curl http://localhost/swagger

# Verificar logs
docker logs qrfinder-nginx
docker logs $(docker ps -q --filter "name=webapi")
```

## 5️⃣ Escalar para 5 réplicas
```bash
# Comando de escala
make scale-up
# ou
docker-compose up -d --scale webapi=5

# Verificar as 5 réplicas
docker ps | grep webapi
# Deve mostrar 5 containers da webapi
```

## 6️⃣ Testar load balancing
```bash
# Fazer múltiplas requisições e ver distribuição
for i in {1..10}; do
  echo "Request $i:"
  curl -s http://localhost/health
  sleep 1
done

# Verificar logs de cada réplica
docker-compose logs webapi | grep -E "(Container|Request)"
```

## 7️⃣ Monitorar réplicas
```bash
# Ver todas as réplicas ativas
docker stats --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}"

# Logs em tempo real de todas as réplicas
docker-compose logs -f webapi
```

## 8️⃣ Escalar para baixo (1 réplica)
```bash
# Voltar para 1 réplica
make scale-down
# ou
docker-compose up -d --scale webapi=1

# Verificar
docker ps | grep webapi
# Deve mostrar apenas 1 container
```

## 9️⃣ Teste de carga (opcional)
```bash
# Instalar Apache Bench se não tiver
# Ubuntu/Debian: sudo apt-get install apache2-utils
# macOS: brew install httpie

# Escalar para 5 réplicas primeiro
make scale-up

# Teste de carga - 1000 requests, 10 concurrent
ab -n 1000 -c 10 http://localhost/health

# Ver como as requisições foram distribuídas
docker-compose logs webapi | grep -c "GET /health"
```

## 🔟 Comandos úteis durante o teste

### Ver status de todos os containers
```bash
docker-compose ps
```

### Ver logs específicos
```bash
# Nginx
docker logs qrfinder-nginx

# WebAPI específica (substitua CONTAINER_ID)
docker logs CONTAINER_ID

# Todas as WebAPIs
docker-compose logs webapi
```

### Reiniciar um serviço específico
```bash
# Reiniciar Nginx
docker-compose restart nginx

# Reiniciar todas as WebAPIs
docker-compose restart webapi
```

### Limpar tudo
```bash
make clean
# ou
docker-compose down -v --remove-orphans
```

## 📊 O que observar

1. **Nginx distribui as requisições** entre as réplicas automaticamente
2. **Cada réplica responde independentemente** - logs diferentes
3. **Escalabilidade instantânea** - sem downtime
4. **Consumo de recursos** aumenta proporcionalmente às réplicas
5. **Load balancer funciona** mesmo com réplicas subindo/descendo

## 🐛 Troubleshooting

### Se o Nginx não encontrar as WebAPIs:
```bash
# Verificar rede
docker network ls
docker network inspect qrfinder_qrfinder-network

# Testar conectividade interna
docker exec qrfinder-nginx nslookup webapi
```

### Se as réplicas não subirem:
```bash
# Ver erros de build
docker-compose logs webapi

# Limpar e rebuildar
docker-compose down
docker-compose build --no-cache webapi
make scale-up
```