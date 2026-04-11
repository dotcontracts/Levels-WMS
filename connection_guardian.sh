#!/bin/bash

# WMS Database Connection Guardian
# Continuous monitoring and cleanup to prevent connection exhaustion
# Safe for production - maintains API compatibility while cleaning up leaked connections

DB_HOST="139.59.12.69"
DB_PORT="5432"
DB_NAME="ficma_wms"
DB_USER="wms_user"
DB_PASS="2019@_Ficma"
LOG_FILE="/tmp/connection_guardian.log"
PID_FILE="/tmp/connection_guardian.pid"

# Configuration
MAX_CONNECTIONS=100
WARNING_THRESHOLD=70  # Alert at 70%
CRITICAL_THRESHOLD=85 # Emergency cleanup at 85%
CHECK_INTERVAL=30     # Check every 30 seconds
IDLE_TIMEOUT=300      # Clean connections idle for >5 minutes
EMERGENCY_TIMEOUT=120 # In emergency, clean connections idle for >2 minutes

# Function to log with timestamp
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

# Function to get connection count
get_connection_count() {
    PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "
    SELECT count(*) FROM pg_stat_activity WHERE datname = '$DB_NAME';" 2>/dev/null | tr -d ' '
}

# Function to get idle connection count
get_idle_count() {
    local timeout_minutes=$1
    PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "
    SELECT count(*) 
    FROM pg_stat_activity 
    WHERE datname = '$DB_NAME' 
    AND state = 'idle' 
    AND now() - state_change > interval '$timeout_minutes minutes'
    AND pid <> pg_backend_pid();" 2>/dev/null | tr -d ' '
}

# Function to terminate idle connections
cleanup_idle_connections() {
    local timeout_minutes=$1
    local description=$2
    
    local idle_count=$(get_idle_count $timeout_minutes)
    
    if [ "$idle_count" -gt 0 ] && [ "$idle_count" != "" ]; then
        log "🧹 $description: Terminating $idle_count idle connections (>$timeout_minutes minutes)"
        
        PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
        SELECT pg_terminate_backend(pid) 
        FROM pg_stat_activity 
        WHERE datname = '$DB_NAME' 
        AND state = 'idle' 
        AND now() - state_change > interval '$timeout_minutes minutes'
        AND pid <> pg_backend_pid();" &>/dev/null
        
        log "✅ Cleaned up $idle_count connections"
        return $idle_count
    else
        log "ℹ️  $description: No idle connections to clean (>$timeout_minutes minutes)"
        return 0
    fi
}

# Function to get connection breakdown
get_connection_breakdown() {
    PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
    SELECT 
        client_addr,
        state,
        count(*) as connections,
        round(avg(extract(epoch from (now() - state_change)))) as avg_idle_seconds
    FROM pg_stat_activity 
    WHERE datname = '$DB_NAME' AND pid <> pg_backend_pid()
    GROUP BY client_addr, state 
    ORDER BY connections DESC;" 2>/dev/null
}

# Signal handlers
cleanup() {
    log "🛑 Connection Guardian shutting down..."
    rm -f "$PID_FILE"
    exit 0
}

trap cleanup SIGTERM SIGINT

# Check if already running
if [ -f "$PID_FILE" ]; then
    OLD_PID=$(cat "$PID_FILE")
    if kill -0 "$OLD_PID" 2>/dev/null; then
        echo "Connection Guardian already running (PID: $OLD_PID)"
        exit 1
    else
        rm -f "$PID_FILE"
    fi
fi

# Start guardian
echo $$ > "$PID_FILE"
log "🚀 WMS Database Connection Guardian started (PID: $$)"
log "📊 Configuration: Warning at ${WARNING_THRESHOLD}%, Critical at ${CRITICAL_THRESHOLD}%"
log "⏰ Check interval: ${CHECK_INTERVAL}s, Idle timeout: ${IDLE_TIMEOUT}s"

# Main monitoring loop
while true; do
    # Get current connection count
    current_connections=$(get_connection_count)
    
    if [ "$current_connections" == "" ] || [ "$current_connections" == "0" ]; then
        log "⚠️  Unable to connect to database, retrying in ${CHECK_INTERVAL}s..."
        sleep $CHECK_INTERVAL
        continue
    fi
    
    # Calculate usage percentage
    usage_percent=$((current_connections * 100 / MAX_CONNECTIONS))
    
    # Determine action based on usage
    if [ $usage_percent -ge $CRITICAL_THRESHOLD ]; then
        log "🚨 CRITICAL: Database usage at ${usage_percent}% (${current_connections}/${MAX_CONNECTIONS})"
        log "🔧 Emergency cleanup initiated..."
        
        # Emergency cleanup - aggressive timeout
        cleanup_idle_connections $((EMERGENCY_TIMEOUT / 60)) "Emergency"
        
        # Also clean very short idle connections if still critical
        sleep 5
        current_connections=$(get_connection_count)
        usage_percent=$((current_connections * 100 / MAX_CONNECTIONS))
        
        if [ $usage_percent -ge $CRITICAL_THRESHOLD ]; then
            log "🆘 Still critical after emergency cleanup, using 1-minute timeout"
            cleanup_idle_connections 1 "Super Emergency"
        fi
        
        log "📊 Post-emergency status: ${usage_percent}% (${current_connections}/${MAX_CONNECTIONS})"
        
    elif [ $usage_percent -ge $WARNING_THRESHOLD ]; then
        log "⚠️  WARNING: Database usage at ${usage_percent}% (${current_connections}/${MAX_CONNECTIONS})"
        
        # Regular cleanup
        cleanup_idle_connections $((IDLE_TIMEOUT / 60)) "Preventive"
        
        # Show connection breakdown
        log "📊 Connection breakdown:"
        get_connection_breakdown | while IFS= read -r line; do
            log "   $line"
        done
        
    else
        # Healthy status - just log periodically
        if [ $(($(date +%s) % 300)) -lt $CHECK_INTERVAL ]; then  # Every 5 minutes
            log "✅ Healthy: ${usage_percent}% usage (${current_connections}/${MAX_CONNECTIONS})"
        fi
        
        # Still do gentle cleanup of very old connections
        cleanup_idle_connections $((IDLE_TIMEOUT / 60)) "Maintenance"
    fi
    
    sleep $CHECK_INTERVAL
done