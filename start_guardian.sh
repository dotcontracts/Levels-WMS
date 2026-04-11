#!/bin/bash

# WMS Connection Guardian Startup Script
# Use this to start/stop/restart the connection guardian

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GUARDIAN_SCRIPT="$SCRIPT_DIR/connection_guardian.sh"
PID_FILE="/tmp/connection_guardian.pid"
LOG_FILE="/tmp/connection_guardian.log"

case "$1" in
    start)
        if [ -f "$PID_FILE" ]; then
            PID=$(cat "$PID_FILE")
            if kill -0 "$PID" 2>/dev/null; then
                echo "✅ Connection Guardian is already running (PID: $PID)"
                exit 0
            else
                echo "🧹 Cleaning up stale PID file..."
                rm -f "$PID_FILE"
            fi
        fi
        
        echo "🚀 Starting WMS Connection Guardian..."
        nohup "$GUARDIAN_SCRIPT" > /dev/null 2>&1 &
        sleep 2
        
        if [ -f "$PID_FILE" ]; then
            PID=$(cat "$PID_FILE")
            echo "✅ Connection Guardian started successfully (PID: $PID)"
            echo "📋 Log file: $LOG_FILE"
            echo "🔍 Monitor with: tail -f $LOG_FILE"
        else
            echo "❌ Failed to start Connection Guardian"
            exit 1
        fi
        ;;
        
    stop)
        if [ -f "$PID_FILE" ]; then
            PID=$(cat "$PID_FILE")
            echo "🛑 Stopping Connection Guardian (PID: $PID)..."
            kill -TERM "$PID" 2>/dev/null
            sleep 3
            
            if kill -0 "$PID" 2>/dev/null; then
                echo "⚠️  Guardian didn't stop gracefully, force killing..."
                kill -KILL "$PID" 2>/dev/null
            fi
            
            rm -f "$PID_FILE"
            echo "✅ Connection Guardian stopped"
        else
            echo "ℹ️  Connection Guardian is not running"
        fi
        ;;
        
    restart)
        echo "🔄 Restarting Connection Guardian..."
        $0 stop
        sleep 2
        $0 start
        ;;
        
    status)
        if [ -f "$PID_FILE" ]; then
            PID=$(cat "$PID_FILE")
            if kill -0 "$PID" 2>/dev/null; then
                echo "✅ Connection Guardian is running (PID: $PID)"
                echo "📊 Recent activity:"
                tail -n 5 "$LOG_FILE" 2>/dev/null || echo "   No log file found"
                
                # Show current database status
                echo ""
                echo "🗄️  Current database status:"
                cd "$SCRIPT_DIR"
                ./database_monitor.sh | grep -E "Current connections|Usage:"
            else
                echo "❌ Connection Guardian PID file exists but process is dead"
                rm -f "$PID_FILE"
                exit 1
            fi
        else
            echo "❌ Connection Guardian is not running"
            exit 1
        fi
        ;;
        
    logs)
        if [ -f "$LOG_FILE" ]; then
            echo "📋 Connection Guardian Logs (last 20 lines):"
            echo "============================================="
            tail -n 20 "$LOG_FILE"
            echo "============================================="
            echo "🔍 Live monitoring: tail -f $LOG_FILE"
        else
            echo "❌ Log file not found: $LOG_FILE"
        fi
        ;;
        
    *)
        echo "WMS Database Connection Guardian Manager"
        echo "========================================"
        echo "Usage: $0 {start|stop|restart|status|logs}"
        echo ""
        echo "Commands:"
        echo "  start   - Start the connection guardian"
        echo "  stop    - Stop the connection guardian"  
        echo "  restart - Restart the connection guardian"
        echo "  status  - Show guardian status and database health"
        echo "  logs    - Show recent guardian activity logs"
        echo ""
        echo "The guardian automatically cleans up idle database connections"
        echo "to prevent 'too many clients' errors in production."
        exit 1
        ;;
esac