#!/bin/bash

# Emergency Database Connection Cleanup
# Run this to immediately clean up long-running idle connections

DB_HOST="139.59.12.69"
DB_PORT="5432"
DB_NAME="ficma_wms"
DB_USER="wms_user"
DB_PASS="2019@_Ficma"

echo "🚨 EMERGENCY CONNECTION CLEANUP - $(date)"
echo "========================================================"

echo "📊 Before cleanup:"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    count(*) as total_connections,
    count(*) FILTER (WHERE state = 'idle') as idle_connections,
    count(*) FILTER (WHERE now() - state_change > interval '5 minutes') as long_idle_connections
FROM pg_stat_activity 
WHERE datname = '$DB_NAME';"

echo ""
echo "🧹 Terminating long-running idle connections (>5 minutes)..."
terminated=$(PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "
SELECT count(*) 
FROM pg_stat_activity 
WHERE datname = '$DB_NAME' 
AND state = 'idle' 
AND now() - state_change > interval '5 minutes'
AND pid <> pg_backend_pid();")

if [ "$terminated" -gt 0 ]; then
    PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
    SELECT pg_terminate_backend(pid) 
    FROM pg_stat_activity 
    WHERE datname = '$DB_NAME' 
    AND state = 'idle' 
    AND now() - state_change > interval '5 minutes'
    AND pid <> pg_backend_pid();" > /dev/null
    
    echo "✅ Terminated $terminated long-running idle connections"
else
    echo "ℹ️  No long-running idle connections to terminate"
fi

echo ""
echo "🧹 Terminating medium-idle connections (>2 minutes)..."
terminated2=$(PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "
SELECT count(*) 
FROM pg_stat_activity 
WHERE datname = '$DB_NAME' 
AND state = 'idle' 
AND now() - state_change > interval '2 minutes'
AND pid <> pg_backend_pid();")

if [ "$terminated2" -gt 0 ]; then
    PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
    SELECT pg_terminate_backend(pid) 
    FROM pg_stat_activity 
    WHERE datname = '$DB_NAME' 
    AND state = 'idle' 
    AND now() - state_change > interval '2 minutes'
    AND pid <> pg_backend_pid();" > /dev/null
    
    echo "✅ Terminated $terminated2 medium-idle connections"
else
    echo "ℹ️  No medium-idle connections to terminate"
fi

echo ""
echo "📊 After cleanup:"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    count(*) as total_connections,
    count(*) FILTER (WHERE state = 'idle') as idle_connections,
    count(*) FILTER (WHERE state = 'active') as active_connections
FROM pg_stat_activity 
WHERE datname = '$DB_NAME';"

echo ""
echo "🎯 Current connection breakdown:"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    client_addr,
    state,
    count(*) as connection_count
FROM pg_stat_activity 
WHERE datname = '$DB_NAME'
AND pid <> pg_backend_pid()
GROUP BY client_addr, state
ORDER BY connection_count DESC;"

echo ""
echo "========================================================"
echo "✅ Emergency cleanup completed - $(date)"