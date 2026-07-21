#!/bin/bash

# Database Connection Monitor and Cleanup Script
# Run this periodically to prevent connection exhaustion

 

echo "🔍 Database Connection Monitor - $(date)"
echo "==============================================="

# Check current connection count
current_connections=$(PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT count(*) FROM pg_stat_activity;")
max_connections=$(PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SHOW max_connections;")

echo "📊 Current connections: $current_connections"
echo "📊 Max connections: $max_connections"

# Calculate percentage
percentage=$((current_connections * 100 / max_connections))
echo "📊 Usage: ${percentage}%"

# Show connection breakdown
echo ""
echo "🔗 Connection Breakdown:"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    state,
    count(*) as connection_count,
    client_addr,
    application_name
FROM pg_stat_activity 
WHERE state IS NOT NULL 
GROUP BY state, client_addr, application_name
ORDER BY connection_count DESC;"

# Warning thresholds
if [ $percentage -gt 80 ]; then
    echo ""
    echo "⚠️  WARNING: Connection usage is ${percentage}% (>80%)"
    echo "🧹 Cleaning up idle connections older than 30 minutes..."
    
    terminated=$(PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "
    SELECT count(*) 
    FROM pg_stat_activity 
    WHERE datname = '$DB_NAME' 
    AND state = 'idle' 
    AND state_change < now() - interval '30 minutes'
    AND pid <> pg_backend_pid();")
    
    if [ "$terminated" -gt 0 ]; then
        PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
        SELECT pg_terminate_backend(pid) 
        FROM pg_stat_activity 
        WHERE datname = '$DB_NAME' 
        AND state = 'idle' 
        AND state_change < now() - interval '30 minutes'
        AND pid <> pg_backend_pid();" > /dev/null
        
        echo "✅ Terminated $terminated idle connections"
    else
        echo "ℹ️  No idle connections to clean up"
    fi
    
elif [ $percentage -gt 90 ]; then
    echo ""
    echo "🚨 CRITICAL: Connection usage is ${percentage}% (>90%)"
    echo "🧹 Emergency cleanup of idle connections older than 10 minutes..."
    
    PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
    SELECT pg_terminate_backend(pid) 
    FROM pg_stat_activity 
    WHERE datname = '$DB_NAME' 
    AND state = 'idle' 
    AND state_change < now() - interval '10 minutes'
    AND pid <> pg_backend_pid();" > /dev/null
    
    echo "✅ Emergency cleanup completed"
else
    echo "✅ Connection usage is healthy (${percentage}%)"
fi

echo ""
echo "📈 Final connection count:"
final_connections=$(PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT count(*) FROM pg_stat_activity;")
echo "📊 Current connections: $final_connections"
echo "==============================================="