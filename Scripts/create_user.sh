 #!/bin/bash

  API_URL="http://localhost:33294"

  # Step 1: Create the user
  echo "Creating user dotcontracts..."
  CREATE_RESPONSE=$(curl -s -X POST "$API_URL/users" \
    -H "Content-Type: application/json" \
    -d '{
      "username": "dotcontracts",
      "email": "dotcontracts@dotcontracts.com",
      "firstName": "Dot",
      "lastName": "Contracts",
      "password": "kibepaul",
      "roleId": 1,
      "branchId": 0
    }')

  echo "Create response: $CREATE_RESPONSE"

  # Extract the user ID
  USER_ID=$(echo "$CREATE_RESPONSE" | grep -o '"id":[0-9]*' | head -1 | grep -o '[0-9]*')

  if [ -z "$USER_ID" ]; then
    echo "Failed to get user ID. Exiting."
    exit 1
  fi

  echo "Created user with ID: $USER_ID"

  # Step 2: Update to enable CanDispatch and CanConfirm
  echo "Enabling dispatch and confirm permissions..."
  UPDATE_RESPONSE=$(curl -s -X PUT "$API_URL/users/$USER_ID" \
    -H "Content-Type: application/json" \
    -d "{
      \"id\": $USER_ID,
      \"username\": \"dotcontracts\",
      \"email\": \"dotcontracts@dotcontracts.com\",
      \"firstName\": \"Dot\",
      \"lastName\": \"Contracts\",
      \"roleId\": 1,
      \"branchId\": 0,
      \"isActive\": true,
      \"canDispatch\": true,
      \"canConfirm\": true
    }")

  echo "Update response: $UPDATE_RESPONSE"
  echo "Done!"