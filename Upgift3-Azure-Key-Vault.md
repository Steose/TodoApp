# Assignment: Azure Key Vault with Managed Identity

**Student:** Osedumme Stephen  
**Course:** CLO25 Grundlaggande Molnapplikationer  
**Submission:** Inlamningsuppgift 3

## Simple Architecture Overview

```text
Local Dev (Laptop)
   |
   | appsettings.json / user secrets / az login
   v
ASP.NET Core App
   |
   | IConfiguration (multiple sources)
   | - JSON files
   | - Environment variables
   | - Key Vault (last, highest priority)
   v
Azure Key Vault
   |
   | MongoDb--ConnectionString
   v
MongoDB / Cosmos DB
```

## Part 1: Code Changes

### Question 1: How Configuration Precedence Works

ASP.NET Core loads configuration from multiple sources in order:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets in development
4. Environment variables
5. Azure Key Vault, if added

Important rule: the last provider added wins when the same key exists in multiple places.

#### What it means that Key Vault is added last

When Key Vault is added last:

```csharp
builder.Configuration.AddAzureKeyVault(...);
```

it overrides all earlier values.

#### Example with `MongoDb:ConnectionString`

`appsettings.Production.json`

```json
{
  "MongoDb": {
    "ConnectionString": "Get from Key Vault"
  }
}
```

Key Vault:

```text
MongoDb--ConnectionString = mongodb://real-prod-db
```

Final result in the app:

```csharp
config["MongoDb:ConnectionString"]
// = "mongodb://real-prod-db"
```

The Key Vault value replaces the dummy value.

### Question 2: Same Key Written in Different Ways

| Source | Format | Example |
| --- | --- | --- |
| `appsettings.json` | `:` | `MongoDb:ConnectionString` |
| Environment variable | `__` | `MongoDb__ConnectionString` |
| Key Vault | `--` | `MongoDb--ConnectionString` |

#### Why they all work

ASP.NET Core normalizes them to the same configuration key:

```text
MongoDb:ConnectionString
```

So this always works:

```csharp
var cs = config["MongoDb:ConnectionString"];
```

It does not matter where the value came from.

### Question 3: Changes Needed to Use Key Vault in Production

#### 1. NuGet packages

```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

- `Azure.Extensions.AspNetCore.Configuration.Secrets` lets ASP.NET Core read secrets from Key Vault.
- `Azure.Identity` handles authentication through `DefaultAzureCredential`.

#### 2. New file

`appsettings.Production.json`

```json
{
  "UseMongoDb": true,
  "UseAzureKeyVault": true,
  "KeyVault": {
    "VaultName": "my-keyvault"
  },
  "MongoDb": {
    "ConnectionString": "Get from Key Vault"
  }
}
```

#### 3. Changes in existing files

`Program.cs`

```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

bool useKeyVault = builder.Configuration.GetValue<bool>("UseAzureKeyVault");
string? vaultName = builder.Configuration["KeyVault:VaultName"];

if (useKeyVault && !string.IsNullOrEmpty(vaultName))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{vaultName}.vault.azure.net/"),
        new DefaultAzureCredential());
}
```

`.csproj`

```xml
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" />
<PackageReference Include="Azure.Identity" />
```

#### 4. Azure setup

1. Create Key Vault.
2. Add secret `MongoDb--ConnectionString`.
3. Enable managed identity on the VM.
4. Give the VM access to Key Vault.

### Question 4: Why "Get from Key Vault" Does Not Break the App

It does not break the app because Key Vault overrides it. The dummy value is never used.

#### What happens if Key Vault is disabled

If:

```text
UseAzureKeyVault = false
UseMongoDb = true
```

then:

```text
ConnectionString = "Get from Key Vault"
```

MongoDB will fail to connect and the application will crash at runtime.

## Part 2: Managed Identity and Azure

### Question 5: Why Managed Identity Is Needed

#### Problem

If secrets are stored in Key Vault, the app still needs credentials to access Key Vault. That creates another secret to manage.

#### Solution: Managed Identity

Managed identity removes the need for passwords.

How it works:

```text
VM -> Azure -> gets token -> Key Vault -> returns secret
```

Benefits:

- No passwords
- No client secrets
- More secure

#### Old vs New

| Old | New |
| --- | --- |
| App stores password | No password |
| Risk of leaks | Safer |
| Manual management | Automatic |

### Question 6: `DefaultAzureCredential` Behavior

The same code is used everywhere:

```csharp
new DefaultAzureCredential();
```

#### In development

It can use:

- Azure CLI login with `az login`
- Visual Studio or VS Code login

#### In production

It can use:

- Managed identity from the VM

#### Why no code change is needed

`DefaultAzureCredential` tries multiple authentication methods automatically.

- Works locally
- Works in Azure

### Question 7: Azure Commands Needed

#### Step 1: Enable managed identity

```bash
az vm identity assign \
  --resource-group MyRG \
  --name MyVm
```

#### Step 2: Give access to Key Vault

```bash
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee <principal-id> \
  --scope <key-vault-resource-id>
```

#### Why this role

- Allows reading secrets only
- Follows least privilege

#### Why not Key Vault Administrator

- Too much access
- Can modify or delete secrets
- Not safe for applications

### Question 8: Why System-Assigned Managed Identity Fits Here

#### System-assigned managed identity

- Created automatically for the VM
- Linked to that VM only
- Deleted when the VM is deleted

#### Why it fits this scenario

- One app
- One VM
- Simple setup

It is easy and secure.

#### Difference from user-assigned identity

| System-assigned | User-assigned |
| --- | --- |
| Tied to one VM | Shared across many resources |
| Auto-deleted | Independent lifecycle |
| Simple | More flexible |

## Final Summary

- ASP.NET Core uses multiple configuration sources, and the last one wins.
- Key Vault overrides local configuration and keeps secrets out of code.
- Managed identity removes the need to store passwords.
- `DefaultAzureCredential` works in both local development and Azure without code changes.

Sources: