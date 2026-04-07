# Deploy TodoApp Infrastructure on Azure with Bicep

## Goal

By the end of this tutorial, you will have deployed a complete TodoApp infrastructure on Azure using Bicep templates. This includes a reverse proxy VM with Nginx, an application VM running ASP.NET Core, networking components, and Cosmos DB for data storage. The application will be accessible via a public IP address.

## Prerequisites

### Required Knowledge
- Basic understanding of Azure concepts (resource groups, virtual networks, VMs)
- Familiarity with command-line interfaces
- Basic understanding of ASP.NET Core applications

### Tools and Software
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) installed and configured
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed
- [Bicep CLI](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/install) (automatically installed via Azure CLI)
- SSH client (built-in on Linux/macOS, or PuTTY on Windows)
- Text editor (VS Code recommended)

### Accounts and Access
- Active Azure subscription with sufficient permissions to create resources
- SSH key pair (RSA or Ed25519) for VM access

## Steps

### 0. Verify Local Development Configuration

Before deploying to Azure, verify the app works locally with Docker MongoDB.

Start MongoDB and mongo-express:

```bash
docker compose -f infra/docker-compose.yml up -d
```

Expected local services:
- MongoDB on `localhost:27017`
- mongo-express on `http://localhost:8081`

The development configuration is expected to use:
- `DatabaseProvider:Provider=MongoDb`
- `MongoDb:ConnectionString=mongodb://localhost:27017`
- `FeatureFlags:UseAzureKeyVault=false`

Run the app locally:

```bash
dotnet run
```

Expected startup output includes:
- `Using MongoDB repository`

Expected local app URL:
- `http://localhost:5292`

### 1. Clone or Prepare the TodoApp Project

First, ensure you have the TodoApp source code ready:

```bash
git clone <your-todoapp-repo-url> TodoApp
cd TodoApp
```

If you don't have a repository, create a new ASP.NET Core MVC application:

```bash
dotnet new mvc -n TodoApp
cd TodoApp
```

### 2. Prepare the Application for Deployment

The application targets `.NET 9.0`. Build and publish it before copying files to the VM:

```bash
dotnet publish TodoApp.csproj -c Release -o ./publish
```

### 3. Set Up Azure Environment

Log in to Azure and set your subscription:

```bash
az login
az account set --subscription "Your Subscription Name"
```

### 4. Navigate to Infrastructure Directory

```bash
cd infra
```

### 5. Configure Deployment Parameters

Edit `main.bicepparam` to set your SSH public key:

```bash
# Get your SSH public key
cat ~/.ssh/id_ed25519.pub

# Replace the placeholder in main.bicepparam with your actual key
# The file should look like:
param sshPublicKey = 'ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAI...your-key-here...'
```

### 6. Deploy the Infrastructure

Run the provisioning script:

```bash
chmod +x provision.sh
./provision.sh
```

This command will:
- Create a resource group named `TodoAppRG`
- Deploy all Azure resources (VMs, networking, Cosmos DB)
- Configure security groups and network rules

### 7. Verify Infrastructure Deployment

Check that resources were created successfully:

```bash
az deployment group show --resource-group TodoAppRG --name main --query "properties.provisioningState"
```

Get the public IP addresses:

```bash
az deployment group show --resource-group TodoAppRG --name main --query "properties.outputs"
```

Expected output includes:
- `reverseProxyPublicIp`: Public IP for accessing the application
- `bastionPublicIp`: IP for SSH access to VMs
- `cosmosEndpoint`: Cosmos DB endpoint

### 8. Understand the Production App Configuration

The production app VM runs the site behind Nginx on plain HTTP port `8080`.

- Nginx terminates external traffic and forwards requests to the app VM.
- The app service should listen on `http://0.0.0.0:8080`.
- The app should use Cosmos DB Mongo API in production, not `mongodb://localhost:27017`.
- The systemd service should launch the app with `ExecStart=/usr/bin/env dotnet /opt/todoapp/TodoApp.dll`.

If you want the production app to read secrets from Azure Key Vault instead of embedding them in `todoapp.service`, enable the `FeatureFlags:UseAzureKeyVault` setting and store the MongoDB connection string in Key Vault using this exact secret name:

- `MongoDb--ConnectionString`

ASP.NET Core maps double dashes in Key Vault secret names to `:` in configuration keys, so `MongoDb--ConnectionString` becomes `MongoDb:ConnectionString`.

The infrastructure template provisions Key Vault with RBAC enabled.

The expected environment entries in `todoapp.service` are:

```ini
Environment="ASPNETCORE_URLS=http://0.0.0.0:8080"
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="DatabaseProvider__Provider=CosmosMongo"
Environment="CosmosMongo__ConnectionString=<full Cosmos Mongo connection string>"
Environment="CosmosMongo__DatabaseName=todoappdb"
Environment="CosmosMongo__TodoCollectionName=Todos"
```

If you use Key Vault for runtime secrets, also configure:

```ini
Environment="FeatureFlags__UseAzureKeyVault=true"
Environment="AzureKeyVault__KeyVaultUri=https://<your-key-vault-name>.vault.azure.net/"
```

### 9. Install .NET Runtime on Application VM

Verify that `dotnet` is installed on the app VM:

```bash
# Get bastion IP from deployment outputs
BASTION_IP=$(az deployment group show --resource-group TodoAppRG --name main --query "properties.outputs.bastionPublicIp.value" -o tsv)

# SSH to app VM via bastion
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "command -v dotnet && dotnet --info"
```

If `dotnet` is missing, add the Microsoft package feed and install the ASP.NET Core runtime:

```bash
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb"
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo dpkg -i /tmp/packages-microsoft-prod.deb && sudo apt-get update"
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo apt-get install -y aspnetcore-runtime-9.0"
```

### 10. Deploy the Application

Run this from your local machine in the `TodoApp` directory to publish, copy the app to the private VM through the bastion host, restart the service, and verify both the backend and public proxy:

```bash
set -euo pipefail

RESOURCE_GROUP="TodoAppRG"
DEPLOYMENT_NAME="main"
APP_VM_IP="10.0.2.4"

dotnet publish TodoApp.csproj -c Release -o ./publish

BASTION_IP=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.bastionPublicIp.value" -o tsv)

PUBLIC_IP=$(az deployment group show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$DEPLOYMENT_NAME" \
  --query "properties.outputs.reverseProxyPublicIp.value" -o tsv)

ssh -J azureuser@"$BASTION_IP" azureuser@"$APP_VM_IP" \
  "sudo systemctl stop todoapp.service || true && sudo mkdir -p /opt/todoapp && sudo chown -R azureuser:azureuser /opt/todoapp"

tar -C publish -cf - . | ssh -J azureuser@"$BASTION_IP" azureuser@"$APP_VM_IP" \
  "tar -C /opt/todoapp -xf -"

ssh -J azureuser@"$BASTION_IP" azureuser@"$APP_VM_IP" \
  "sudo systemctl daemon-reload && sudo systemctl restart todoapp.service && sudo systemctl status todoapp.service --no-pager"

ssh -J azureuser@"$BASTION_IP" azureuser@"$APP_VM_IP" \
  "curl -I http://127.0.0.1:8080"

curl -I "http://$PUBLIC_IP"
echo "TodoApp URL: http://$PUBLIC_IP"
```

Copy the published application to the VM:

```bash
# From your local machine (in TodoApp directory)
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo systemctl stop todoapp.service || true && sudo rm -rf /opt/todoapp && sudo mkdir -p /opt/todoapp && sudo chown -R azureuser:azureuser /opt/todoapp"
tar -C publish -cf - . | ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "tar -C /opt/todoapp -xf -"
```

Write the systemd service with the Cosmos configuration:

```bash
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 'sudo tee /etc/systemd/system/todoapp.service > /dev/null <<EOF
[Unit]
Description=TodoApp ASP.NET Core Application
After=network.target

[Service]
Type=simple
User=azureuser
WorkingDirectory=/opt/todoapp
ExecStart=/usr/bin/env dotnet /opt/todoapp/TodoApp.dll
Restart=on-failure
RestartSec=5
StandardOutput=inherit
StandardError=inherit
Environment="ASPNETCORE_URLS=http://0.0.0.0:8080"
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="DatabaseProvider__Provider=CosmosMongo"
Environment="CosmosMongo__ConnectionString=<full Cosmos Mongo connection string>"
Environment="CosmosMongo__DatabaseName=todoappdb"
Environment="CosmosMongo__TodoCollectionName=Todos"

[Install]
WantedBy=multi-user.target
EOF
sudo systemctl daemon-reload
sudo systemctl enable todoapp.service
sudo systemctl restart todoapp.service'
```

## Verification

### Test Application Access

Get the public IP and test the application:

```bash
# Get the reverse proxy public IP
PUBLIC_IP=$(az deployment group show --resource-group TodoAppRG --name main --query "properties.outputs.reverseProxyPublicIp.value" -o tsv)

# Test the application
curl http://$PUBLIC_IP
```

Expected result: HTML content showing the TodoApp home page with "Welcome" message.

Verify the Todo page specifically:

```bash
curl http://$PUBLIC_IP/Todo
```

Expected result: HTML for the Todo list page. If the database is empty, the page should show `No todo items found.` instead of a connection error.

### Verify Infrastructure Components

Check that all components are running:

```bash
# Check VM status
az vm list -g TodoAppRG -o table

# Check Cosmos DB
az cosmosdb list -g TodoAppRG -o table

# Verify network security groups
az network nsg list -g TodoAppRG -o table
```

### Check Application Logs

Monitor the application service:

```bash
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo systemctl status todoapp.service --no-pager"
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo journalctl -u todoapp.service -f"
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo cat /etc/systemd/system/todoapp.service"
```

## Cleanup

To avoid ongoing Azure charges, delete all resources when you're done:

```bash
# Delete the entire resource group
az group delete --resource-group TodoAppRG --yes --no-wait
```

This will remove:
- All virtual machines
- Virtual networks and subnets
- Public IP addresses
- Network security groups
- Cosmos DB account
- All associated resources

**Warning:** This action cannot be undone. Make sure you have backed up any important data from Cosmos DB before deleting.

### Alternative: Delete Individual Resources

If you want to keep some resources:

```bash
# Delete VMs only
az vm delete -g TodoAppRG -n demo-bastion-vm --yes
az vm delete -g TodoAppRG -n demo-proxy-vm --yes
az vm delete -g TodoAppRG -n demo-app-vm --yes

# Delete Cosmos DB
az cosmosdb delete -g TodoAppRG -n todoappcosmosmongo12345 --yes
```

## Troubleshooting

### Common Issues

**SSH Connection Failed**
- Verify your SSH key is correctly added to `main.bicepparam`
- Check that the bastion VM is running: `az vm list -g TodoAppRG`

**Application Not Accessible**
- Verify the TodoApp service is running on the app VM
- Check nginx configuration on the proxy VM
- Ensure network security groups allow traffic on ports 80 and 22
- Verify the app is listening on `0.0.0.0:8080`
- Verify `todoapp.service` contains the Cosmos environment variables

**Bicep Deployment Failed**
- Check Azure CLI login: `az account show`
- Verify resource quotas in your subscription
- Review deployment errors: `az deployment group show -g TodoAppRG -n main`

**App Starts But Database Fails**
- If logs mention `localhost:27017`, the VM is still using the default `MongoDb` settings instead of Cosmos
- Check `/etc/systemd/system/todoapp.service` for `DatabaseProvider__Provider=CosmosMongo`
- Verify the Cosmos connection string and database name are correct

**Service Fails With `203/EXEC`**
- `dotnet` is missing or the `ExecStart` path is wrong
- Verify `command -v dotnet`
- Use `ExecStart=/usr/bin/env dotnet /opt/todoapp/TodoApp.dll`

**Permission Denied on provision.sh**
- Run `chmod +x provision.sh` to make the script executable

For additional help, check the Azure Resource Manager deployment operations:
```bash
az deployment operation group list -g TodoAppRG -n main --query "[?properties.provisioningState!='Succeeded']"
```
