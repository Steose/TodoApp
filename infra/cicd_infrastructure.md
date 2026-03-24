# Setting Up CI/CD for TodoApp with GitHub Actions

## Goal

By the end of this tutorial, you will have a complete CI/CD pipeline that automatically builds, tests, and deploys your TodoApp to Azure infrastructure using GitHub Actions with a self-hosted runner. The pipeline will trigger on code changes and provide automated deployment to your production environment.

## Prerequisites

### Required Knowledge
- Basic understanding of Git and GitHub
- Familiarity with Azure infrastructure (from the Bicep infrastructure tutorial)
- Basic understanding of CI/CD concepts

### Tools and Software
- GitHub repository with TodoApp code
- Azure infrastructure deployed (from previous tutorial)
- SSH access to your Azure VMs
- GitHub Actions enabled on your repository

### Accounts and Access
- GitHub repository with admin access
- Azure subscription with deployed infrastructure
- SSH access to bastion and app VMs

## Steps

### 1. Verify Infrastructure is Running

Ensure your Azure infrastructure is deployed and accessible:

```bash
# Check deployment status
az deployment group show --resource-group TodoAppRG --name main --query "properties.provisioningState"

# Get IP addresses
az deployment group show --resource-group TodoAppRG --name main --query "properties.outputs"
```

### 2. Set Up Self-Hosted Runner on App VM

Connect to your app VM via bastion and install the GitHub Actions runner:

```bash
# Get bastion IP
BASTION_IP=$(az deployment group show --resource-group TodoAppRG --name main --query "properties.outputs.bastionPublicIp.value" -o tsv)

# SSH to app VM
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4

# On the app VM, create runner directory
mkdir -p actions-runner && cd actions-runner

# Download the latest runner package
curl -o actions-runner-linux-x64-2.333.0.tar.gz -L https://github.com/actions/runner/releases/download/v2.333.0/actions-runner-linux-x64-2.333.0.tar.gz

# Extract the installer
tar xzf ./actions-runner-linux-x64-2.333.0.tar.gz
```

### 3. Configure the Runner

In your GitHub repository, go to Settings → Actions → Runners and click "New self-hosted runner". Follow the instructions to get the registration token:

```bash
# On the app VM, configure the runner (replace with your actual token)
./config.sh --url https://github.com/YOUR_USERNAME/YOUR_REPO --token YOUR_REGISTRATION_TOKEN --name "azure-runner" --labels "self-hosted,azure" --work _work --unattended
```

### 4. Install Runner as Service

Set up the runner to run as a systemd service:

```bash
# Create service file
sudo ./svc.sh install

# Start the service
sudo ./svc.sh start

# Check status
sudo ./svc.sh status
```

### 5. Create GitHub Actions Workflow

Create the workflow file in your repository:

```bash
# Create the workflows directory
mkdir -p .github/workflows

# Create the CI/CD workflow file
touch .github/workflows/cicd.yaml
```

### 6. Configure the Workflow

Add the following content to `.github/workflows/cicd.yaml`:

```yaml
name: TodoApp CI/CD

on:
  push:
    branches:
    - "main"
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest
    steps:

    - name: Install .NET SDK
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Check out this repo
      uses: actions/checkout@v4

    - name: Restore dependencies
      run: dotnet restore

    - name: Build and publish the app
      run: |
        dotnet build --no-restore
        dotnet publish -c Release -o ./publish

    - name: Upload app artifacts
      uses: actions/upload-artifact@v4
      with:
        name: app-artifacts
        path: ./publish

  deploy:
    runs-on: self-hosted
    needs: build
    if: github.ref == 'refs/heads/main'

    steps:
    - name: Download artifacts
      uses: actions/download-artifact@v4
      with:
        name: app-artifacts

    - name: Verify downloaded files
      run: |
        echo "Current directory contents:"
        ls -la
        echo "Publish directory contents:"
        ls -la ./publish/

    - name: Stop the application service
      run: |
        sudo systemctl stop todoapp.service || true

    - name: Deploy the application
      run: |
        sudo rm -rf /opt/todoapp
        sudo mkdir -p /opt/todoapp
        sudo cp -r ./publish/* /opt/todoapp/
        sudo chown -R azureuser:azureuser /opt/todoapp

    - name: Start the application service
      run: |
        sudo systemctl daemon-reload
        sudo systemctl start todoapp.service

    - name: Check service status
      run: |
        sudo systemctl status todoapp.service --no-pager
        echo "Service logs:"
        journalctl -u todoapp.service -n 20 --no-pager
```

### 7. Commit and Push Changes

```bash
git add .
git commit -m "Add CI/CD pipeline with GitHub Actions"
git push origin main
```

### 8. Monitor the Pipeline

Go to your GitHub repository → Actions tab to see the workflow running. The pipeline will:

1. **Build job**: Run on GitHub's hosted runners
   - Install .NET 9.0
   - Checkout code
   - Restore dependencies
   - Build and publish app
   - Upload artifacts

2. **Deploy job**: Run on your self-hosted runner
   - Download artifacts
   - Stop the app service
   - Deploy new files
   - Start the app service
   - Verify deployment

## Verification

### Check Pipeline Execution

1. Go to GitHub → Your Repository → Actions
2. Click on the latest workflow run
3. Verify both jobs completed successfully

### Test Application Deployment

```bash
# Get the public IP
PUBLIC_IP=$(az deployment group show --resource-group TodoAppRG --name main --query "properties.outputs.reverseProxyPublicIp.value" -o tsv)

# Test the application
curl http://$PUBLIC_IP
```

Expected result: Your TodoApp home page HTML.

### Verify Runner Status

Check that your self-hosted runner is online:

```bash
# SSH to app VM
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4

# Check runner service
sudo ./actions-runner/svc.sh status

# Check runner logs
tail -f /home/azureuser/actions-runner/_diag/Runner_*.log
```

## Cleanup

### Remove CI/CD Pipeline

To disable the CI/CD pipeline:

```bash
# Remove workflow file
rm .github/workflows/cicd.yaml
git add .
git commit -m "Remove CI/CD pipeline"
git push origin main
```

### Remove Self-Hosted Runner

To remove the runner from your VM:

```bash
# SSH to app VM
ssh -J azureuser@$BASTION_IP azureuser@10.0.2.4

# Stop and remove service
sudo ./actions-runner/svc.sh stop
sudo ./actions-runner/svc.sh uninstall

# Remove runner files
rm -rf actions-runner
```

### Remove Infrastructure

To clean up all Azure resources (from the infrastructure tutorial):

```bash
az group delete --resource-group TodoAppRG --yes
```

## Troubleshooting

### Common Issues

**Runner Not Connecting**
- Verify SSH access to the VM
- Check that the registration token is valid (expires after 1 hour)
- Ensure the VM has internet access

**Build Job Failing**
- Check .NET version compatibility (workflow uses 9.0.x, project targets net9.0)
- Verify all dependencies are properly committed
- Check build logs for specific errors

**Deploy Job Failing**
- Verify the systemd service name matches (`todoapp.service`)
- Check file permissions on `/opt/todoapp`
- Ensure the app VM has .NET runtime installed

**Application Not Starting**
- Check service logs: `journalctl -u todoapp.service`
- Verify the published files are in the correct location
- Test manually: `cd /opt/todoapp && dotnet TodoApp.dll`

### Debug Commands

```bash
# Check runner status
sudo ./actions-runner/svc.sh status

# View runner logs
tail -f /home/azureuser/actions-runner/_diag/Runner_*.log

# Check service status
sudo systemctl status todoapp.service

# View application logs
journalctl -u todoapp.service -f

# Test application manually
cd /opt/todoapp
dotnet TodoApp.dll --urls "http://0.0.0.0:8080"
```

### Security Considerations

- Self-hosted runners should be in a private network
- Use GitHub secrets for sensitive configuration
- Regularly update the runner software
- Monitor runner usage and costs

For additional help, check the GitHub Actions documentation and Azure deployment logs. The pipeline provides detailed logs for each step to help diagnose issues.