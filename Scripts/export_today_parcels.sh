#!/bin/bash
# export_today_parcels.sh
# Downloads today's parcels from production PostgreSQL to a CSV file

DB_HOST="139.59.12.69"
DB_PORT="5432"
DB_NAME="ficma_wms"
DB_USER="wms_user"
DB_PASS="2019@_Ficma"

OUTPUT_FILE="parcels_today_$(date +%Y-%m-%d).csv"

PGPASSWORD="$DB_PASS" psql \
  -h "$DB_HOST" \
  -p "$DB_PORT" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -c "\COPY (
    SELECT 
      p.\"Id\",
      p.\"CreatedAt\" AT TIME ZONE 'Africa/Nairobi' AS \"CreatedAt\",
      p.\"WaybillNumber\",
      p.\"QRCode\",
      p.\"Sender\",
      p.\"SenderTelephone\",
      p.\"Receiver\",
      p.\"ReceiverTelephone\",
      p.\"Destination\",
      p.\"Quantity\",
      p.\"Description\",
      p.\"Amount\",
      p.\"Rate\",
      p.\"PaymentMethods\",
      p.\"TotalAmount\",
      p.\"TotalRate\",
      p.\"AmountPaid\",
      p.\"TransactionCode\",
      p.\"Status\",
      p.\"DispatchedAt\" AT TIME ZONE 'Africa/Nairobi' AS \"DispatchedAt\",
      p.\"DispatchTrackingCode\",
      p.\"ConfirmedAt\" AT TIME ZONE 'Africa/Nairobi' AS \"ConfirmedAt\"
    FROM \"Parcels\" p
    WHERE (p.\"CreatedAt\" AT TIME ZONE 'Africa/Nairobi')::date = CURRENT_DATE
    ORDER BY p.\"CreatedAt\" DESC
  ) TO '$OUTPUT_FILE' CSV HEADER"

echo "Export complete: $OUTPUT_FILE"
