# 🚀 Deployment Guide - Digital Ocean + Azure

Guia simples para deploy do QrFinder na Digital Ocean usando Azure Blob Storage.

## 📋 Pré-requisitos

### Digital Ocean VPS
- Ubuntu 22.04 LTS
- 2GB RAM mínimo (4GB recomendado)
- Docker e Docker Compose instalados

### Azure Storage Account
- Conta do Azure (da escola)
- Storage Account criado
- Container "videos" criado
- Connection String disponível

## 🔧 Setup da VPS

### 1. Instalar Docker
```bash
# Atualizar sistema
sudo apt update && sudo apt upgrade -y

# Instalar Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# Instalar Docker Compose
sudo apt install docker-compose-plugin -y

# Logout e login novamente para aplicar grupo docker
```

### 2. Preparar ambiente
```bash
# Criar diretório da aplicação
sudo mkdir -p /opt/qrfinder
sudo chown $USER:$USER /opt/qrfinder
cd /opt/qrfinder

# Clonar código (ou copiar arquivos necessários)
git clone https://github.com/seu-usuario/qrfinder.git /tmp/qrfinder
cp /tmp/qrfinder/docker-compose.prod.yml .
cp -r /tmp/qrfinder/infra .
```

### 3. Configurar variáveis de ambiente
```bash
# Criar arquivo .env
nano .env
```

Conteúdo do .env:
```bash
GITHUB_REPOSITORY=seu-usuario/qrfinder
MONGO_PASSWORD=sua-senha-segura-aqui
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=...
AZURE_STORAGE_CONTAINER_NAME=videos
```

## 🚀 Deploy Manual

### Primeira instalação:
```bash
cd /opt/qrfinder

# Baixar imagens
docker-compose -f docker-compose.prod.yml pull

# Subir serviços
docker-compose -f docker-compose.prod.yml up -d

# Verificar status
docker-compose -f docker-compose.prod.yml ps
```

### Atualizações:
```bash
# Baixar novas versões
docker-compose -f docker-compose.prod.yml pull

# Restart com novas imagens
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d
```

## 🔄 CI/CD Automático (GitHub Actions)

### 1. Configurar Secrets no GitHub
No seu repositório, vá em Settings > Secrets and variables > Actions:

```bash
DO_HOST=ip.da.sua.vps
DO_USERNAME=seu-usuario
DO_SSH_KEY=sua-chave-ssh-privada
AZURE_STORAGE_CONNECTION_STRING=sua-connection-string
MONGO_PASSWORD=sua-senha-mongo
```

### 2. Deploy automático
- Push para branch `main` → deploy automático
- Pull requests → apenas testes

## 📊 Monitoramento

### Verificar status:
```bash
docker-compose -f docker-compose.prod.yml ps
```

### Ver logs:
```bash
# Todos os serviços
docker-compose -f docker-compose.prod.yml logs -f

# Serviço específico
docker-compose -f docker-compose.prod.yml logs -f webapi
```

### Verificar saúde:
```bash
curl http://localhost/health
```

## 🔧 Troubleshooting

### Serviços não iniciam:
```bash
# Ver logs detalhados
docker-compose -f docker-compose.prod.yml logs

# Restart específico
docker-compose -f docker-compose.prod.yml restart webapi
```

### Problemas de memória:
```bash
# Verificar uso
docker stats
free -h

# Limpar imagens antigas
docker image prune -f
docker system prune -f
```

### Azure Storage não conecta:
```bash
# Testar connection string
docker run --rm -it mcr.microsoft.com/azure-cli az storage container list --connection-string "sua-connection-string"
```

## 🌐 Acessos

Após deploy bem-sucedido:
- **App**: http://ip-da-vps/app/
- **API**: http://ip-da-vps/
- **Swagger**: http://ip-da-vps/swagger/

## 💡 Dicas

1. **Firewall**: Libere porta 80 no firewall da Digital Ocean
2. **Domínio**: Configure um domínio apontando para o IP da VPS
3. **SSL**: Use Cloudflare ou Let's Encrypt para HTTPS
4. **Backup**: Configure backup automático do MongoDB
5. **Monitoramento**: Use Uptime Robot ou similar