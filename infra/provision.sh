#!/usr/bin/env bash
set -euo pipefail

RESOURCE_GROUP="TodoAppRG"
LOCATION="northeurope"

echo "Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION"

echo "Deploying Bicep..."
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file main.bicep \
  --parameters main.bicepparam

echo "Deployment complete."

echo "Outputs:"
az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$(az deployment group list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv)" \
  --query "properties.outputs"