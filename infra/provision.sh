

#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="TodoAppRG"
LOCATION="northeurope"

echo "Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION"

echo "Getting signed-in user object ID..."
CURRENT_USER_OBJECT_ID=$(az ad signed-in-user show --query id --output tsv)

echo "Deploying Bicep..."
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file main.bicep \
  --parameters main.bicepparam \
  --parameters currentUserObjectId="$CURRENT_USER_OBJECT_ID"

echo "Deployment complete."

echo "Outputs:"
az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$(az deployment group list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv)" \
  --query "properties.outputs"

echo "Note: Key Vault RBAC role assignment may take a short time to propagate."  