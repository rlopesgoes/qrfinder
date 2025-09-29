#!/bin/bash

# QrFinder Load Test Monitor
# Monitors system resources and Kafka during load testing

echo "🔍 QrFinder Load Test Monitor"
echo "============================="
echo ""

# Check if docker-compose is running
if ! docker-compose ps | grep -q "Up"; then
    echo "❌ Docker Compose services are not running!"
    echo "Run: docker-compose up -d"
    exit 1
fi

# Function to get container stats
get_container_stats() {
    local container=$1
    local stats=$(docker stats $container --no-stream --format "table {{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}")
    echo "$stats" | tail -n +2
}

# Function to get Kafka topic info
get_kafka_info() {
    docker exec qrfinder-kafka kafka-topics --bootstrap-server localhost:29092 --describe --topic $1 2>/dev/null | grep -E "PartitionCount|ReplicationFactor"
}

# Function to get Kafka consumer lag
get_consumer_lag() {
    docker exec qrfinder-kafka kafka-consumer-groups --bootstrap-server localhost:29092 --describe --group $1 2>/dev/null | grep -v "GROUP\|TOPIC" | awk '{print $5}' | paste -sd+ | bc 2>/dev/null || echo "0"
}

# Function to count messages in topic
count_messages() {
    local topic=$1
    local count=$(docker exec qrfinder-kafka kafka-run-class kafka.tools.GetOffsetShell --broker-list localhost:29092 --topic $topic --time -1 2>/dev/null | awk -F':' '{sum += $3} END {print sum}')
    echo ${count:-0}
}

# Function to get MongoDB stats
get_mongo_stats() {
    docker exec qrfinder-mongo mongosh --quiet --eval "
        use qrfinder;
        print('Collections:');
        db.getCollectionNames().forEach(function(collection) {
            var count = db[collection].countDocuments();
            print('  ' + collection + ': ' + count + ' docs');
        });
    " 2>/dev/null
}

# Main monitoring loop
echo "🚀 Starting monitoring... (Press Ctrl+C to stop)"
echo ""

while true; do
    clear
    echo "🔍 QrFinder Load Test Monitor - $(date '+%H:%M:%S')"
    echo "================================================="
    echo ""
    
    # System Resources
    echo "💻 CONTAINER RESOURCES"
    echo "----------------------"
    printf "%-20s %-10s %-20s %-15s\n" "CONTAINER" "CPU%" "MEMORY" "NETWORK I/O"
    echo "--------------------------------------------------------------------"
    
    for container in qrfinder-webapi qrfinder-webapp qrfinder-analysis-worker qrfinder-results-worker qrfinder-notifications-worker qrfinder-signalr-server; do
        if docker ps --format "{{.Names}}" | grep -q "^$container$"; then
            stats=$(get_container_stats $container)
            printf "%-20s %s\n" "${container#qrfinder-}" "$stats"
        fi
    done
    echo ""
    
    # Kafka Metrics
    echo "📊 KAFKA METRICS"
    echo "----------------"
    
    # Topic message counts
    echo "📨 Topic Message Counts:"
    for topic in "video.analysis.queue" "video.progress" "videos.results" "video.progress.notifications"; do
        count=$(count_messages $topic)
        printf "  %-25s: %10s messages\n" "$topic" "$count"
    done
    echo ""
    
    # Consumer group lag
    echo "⏳ Consumer Group Lag:"
    for group in "analysis-group" "results-group" "notifications-group"; do
        lag=$(get_consumer_lag $group)
        printf "  %-20s: %10s messages behind\n" "$group" "$lag"
    done
    echo ""
    
    # MongoDB Stats
    echo "🗄️  MONGODB STATS"
    echo "----------------"
    get_mongo_stats
    echo ""
    
    # System Load
    echo "⚡ SYSTEM LOAD"
    echo "-------------"
    if command -v uptime >/dev/null; then
        uptime
    fi
    if command -v free >/dev/null; then
        free -h | head -2
    fi
    echo ""
    
    # Docker Stats Summary
    echo "🐳 DOCKER SUMMARY"
    echo "----------------"
    running_containers=$(docker ps --filter "name=qrfinder" --format "{{.Names}}" | wc -l)
    echo "Running containers: $running_containers"
    
    total_cpu=$(docker stats --no-stream --format "{{.CPUPerc}}" | grep -o "[0-9.]*" | awk '{sum += $1} END {print sum}')
    echo "Total CPU usage: ${total_cpu:-0}%"
    echo ""
    
    echo "📋 Press Ctrl+C to stop monitoring"
    echo "🔄 Refreshing in 5 seconds..."
    
    sleep 5
done