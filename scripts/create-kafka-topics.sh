#!/bin/bash

echo "🔧 Criando tópicos Kafka com 5 partições para video.analysis.queue..."

# Wait for Kafka to be ready
echo "⏳ Aguardando Kafka estar pronto..."
docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:29092 --list > /dev/null 2>&1
while [ $? -ne 0 ]; do
    echo "Kafka não está pronto, aguardando..."
    sleep 2
    docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:29092 --list > /dev/null 2>&1
done

echo "✅ Kafka está pronto! Criando tópicos..."

# Create topics with optimized partitions
docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:29092 --create --if-not-exists --topic video.analysis.queue --partitions 5 --replication-factor 1
docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:29092 --create --if-not-exists --topic video.progress --partitions 1 --replication-factor 1
docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:29092 --create --if-not-exists --topic videos.results --partitions 1 --replication-factor 1
docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:29092 --create --if-not-exists --topic video.progress.notifications --partitions 1 --replication-factor 1

echo "📋 Listando tópicos criados:"
docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:29092 --list

echo "📊 Detalhes do tópico principal:"
docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:29092 --describe --topic video.analysis.queue

echo "✅ Tópicos criados com sucesso! 5 partições no video.analysis.queue para 5 workers paralelos."