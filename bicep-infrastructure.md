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

Update the project to target .NET 9.0 (compatible with current Azure environments):

```bash
# Edit TodoApp.csproj
# Change <TargetFramework>net10.0</TargetFramework> to:
# <TargetFramework>net9.0</TargetFramework>
```

Build and publish the application:

```bash
dotnet publish -c Release -o ./publish
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
- `cosmosEndpoint`: Connection string for database

### 8. Install .NET Runtime on Application VM

The infrastructure includes automated setup, but verify .NET installation:

```bash
# Get bastion IP from deployment outputs
BASTION_IP=$(az deployment group show --resource-group TodoAppRG --name main --query "properties.outputs.bastionPublicIp.value" -o tsv)

# SSH to app VM via bastion
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "dotnet --version"
```

If .NET is not installed, install it manually:

```bash
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo apt-get update && sudo apt-get install -y wget"
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb"
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo dpkg -i packages-microsoft-prod.deb && sudo apt-get update"
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo apt-get install -y dotnet-sdk-9.0 dotnet-runtime-9.0"
```

### 9. Deploy the Application

Copy the published application to the VM:

```bash
# From your local machine (in TodoApp directory)
scp -o ProxyCommand="ssh -W %h:%p azureuser@$BASTION_IP" -r ./publish/* azureuser@10.0.2.4:/opt/todoapp/
```

Start the application service:

```bash
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "sudo systemctl start todoapp"
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
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "systemctl status todoapp"
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4 "journalctl -u todoapp -f"
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

**Bicep Deployment Failed**
- Check Azure CLI login: `az account show`
- Verify resource quotas in your subscription
- Review deployment errors: `az deployment group show -g TodoAppRG -n main`

**Permission Denied on provision.sh**
- Run `chmod +x provision.sh` to make the script executable

For additional help, check the Azure Resource Manager deployment operations:
```bash
az deployment operation group list -g TodoAppRG -n main --query "[?properties.provisioningState!='Succeeded']"
```
