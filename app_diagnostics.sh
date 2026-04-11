#!/bin/bash

# WMS Application Diagnostics Script
# Run this to diagnose app crashes beyond database connectivity

DB_HOST="139.59.12.69"
DB_PORT="5432"
DB_NAME="ficma_wms"
DB_USER="wms_user"
DB_PASS="2019@_Ficma"

echo "🔍 WMS Application Diagnostics - $(date)"
echo "==========================================="

# 1. Check for expensive/long-running queries
echo ""
echo "📊 Long-Running Queries (>5 seconds):"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    pid,
    now() - pg_stat_activity.query_start AS duration,
    client_addr,
    application_name,
    state,
    left(query, 100) as query_preview
FROM pg_stat_activity 
WHERE (now() - pg_stat_activity.query_start) > interval '5 seconds'
AND query NOT LIKE '%pg_stat_activity%'
ORDER BY duration DESC;"

# 2. Check table sizes (large tables can cause memory issues)
echo ""
echo "📈 Database Table Sizes:"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) as size,
    pg_relation_size(schemaname||'.'||tablename) as size_bytes
FROM pg_tables 
WHERE schemaname NOT IN ('information_schema', 'pg_catalog')
ORDER BY pg_relation_size(schemaname||'.'||tablename) DESC
LIMIT 10;"

# 3. Check for table locks (can cause timeouts)
echo ""
echo "🔒 Current Table Locks:"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    t.relname,
    l.locktype,
    page,
    virtualtransaction,
    pid,
    mode,
    granted
FROM pg_locks l, pg_stat_all_tables t 
WHERE l.relation = t.relid 
ORDER BY relation ASC;"

# 4. Check for blocked queries
echo ""
echo "⚠️ Blocked Queries:"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    blocked_locks.pid AS blocked_pid,
    blocked_activity.usename AS blocked_user,
    blocking_locks.pid AS blocking_pid,
    blocking_activity.usename AS blocking_user,
    blocked_activity.query AS blocked_statement,
    blocking_activity.query AS current_statement_in_blocking_process
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks 
    ON blocking_locks.locktype = blocked_locks.locktype
    AND blocking_locks.database IS NOT DISTINCT FROM blocked_locks.database
    AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
    AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
    AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
    AND blocking_locks.virtualxid IS NOT DISTINCT FROM blocked_locks.virtualxid
    AND blocking_locks.transactionid IS NOT DISTINCT FROM blocked_locks.transactionid
    AND blocking_locks.classid IS NOT DISTINCT FROM blocked_locks.classid
    AND blocking_locks.objid IS NOT DISTINCT FROM blocked_locks.objid
    AND blocking_locks.objsubid IS NOT DISTINCT FROM blocked_locks.objsubid
    AND blocking_locks.pid != blocked_locks.pid
JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;"

# 5. Check memory usage patterns
echo ""
echo "💾 Database Memory Usage:"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    name,
    setting,
    unit,
    short_desc
FROM pg_settings 
WHERE name IN ('shared_buffers', 'work_mem', 'maintenance_work_mem', 'effective_cache_size', 'max_connections')
ORDER BY name;"

# 6. Check recent connection errors
echo ""
echo "🚨 Recent Connection Errors:"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    client_addr,
    count(*) as connection_attempts,
    min(backend_start) as first_attempt,
    max(backend_start) as last_attempt,
    array_agg(DISTINCT application_name) as app_names
FROM pg_stat_activity 
WHERE backend_start > now() - interval '1 hour'
GROUP BY client_addr
ORDER BY connection_attempts DESC;"

# 7. Check for frequent disconnections
echo ""
echo "📡 Connection Patterns (Last Hour):"
PGPASSWORD="$DB_PASS" psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "
SELECT 
    client_addr,
    state,
    count(*) as count,
    avg(EXTRACT(EPOCH FROM (now() - backend_start))) as avg_duration_seconds
FROM pg_stat_activity 
WHERE backend_start > now() - interval '1 hour'
AND datname = '$DB_NAME'
GROUP BY client_addr, state
ORDER BY client_addr, count DESC;"

echo ""
echo "==========================================="
echo "✅ Diagnostics completed - $(date)"