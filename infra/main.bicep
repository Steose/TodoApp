targetScope = 'resourceGroup'

@description('Deployment location')
param location string = resourceGroup().location

@description('Prefix used for resource names')
param prefix string = 'demo'

@description('Admin username for the Linux VMs')
param adminUsername string = 'azureuser'

@description('SSH public key for the Linux VMs')
param sshPublicKey string

@description('VM size for all VMs')
param vmSize string = 'Standard_D2s_v3'

@description('Ubuntu image publisher')
param imagePublisher string = 'Canonical'

@description('Ubuntu image offer')
param imageOffer string = '0001-com-ubuntu-server-jammy'

@description('Ubuntu image SKU')
param imageSku string = '22_04-lts-gen2'

@description('Cosmos DB account name. Must be globally unique, lowercase, 3-44 chars.')
param cosmosAccountName string

@description('Mongo database name')
param cosmosMongoDbName string = 'appdb'

@description('Key Vault name. Must be globally unique.')
param keyVaultName string

@description('Object ID of the signed-in Azure user who should become Key Vault Administrator.')
param currentUserObjectId string = ''

var vnetName = '${prefix}-vnet'

var bastionSubnetName = 'bastion-subnet'
var proxySubnetName = 'proxy-subnet'
var appSubnetName = 'app-subnet'

var bastionSubnetPrefix = '10.0.0.0/24'
var proxySubnetPrefix = '10.0.1.0/24'
var appSubnetPrefix = '10.0.2.0/24'

var bastionNsgName = '${prefix}-bastion-nsg'
var proxyNsgName = '${prefix}-proxy-nsg'
var appNsgName = '${prefix}-app-nsg'

var bastionAsgName = '${prefix}-bastion-asg'
var proxyAsgName = '${prefix}-proxy-asg'
var appAsgName = '${prefix}-app-asg'

var bastionPipName = '${prefix}-bastion-pip'
var proxyPipName = '${prefix}-proxy-pip'

var bastionNicName = '${prefix}-bastion-nic'
var proxyNicName = '${prefix}-proxy-nic'
var appNicName = '${prefix}-app-nic'

var bastionVmName = '${prefix}-bastion-vm'
var proxyVmName = '${prefix}-proxy-vm'
var appVmName = '${prefix}-app-vm'

var cosmosPrimaryKey = cosmosAccount.listKeys().primaryMasterKey
var cosmosMongoUsername = uriComponent(cosmosAccount.name)
var cosmosMongoPassword = uriComponent(cosmosPrimaryKey)
var cosmosMongoConnectionString = 'mongodb://${cosmosMongoUsername}:${cosmosMongoPassword}@${cosmosAccount.name}.mongo.cosmos.azure.com:10255/${cosmosMongoDbName}?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&authMechanism=SCRAM-SHA-256&appName=@${cosmosAccount.name}@'
var appInitTemplate = loadTextContent('cloud-init-dotnet-app.yaml')
var appInitWithConnection = replace(appInitTemplate, '__COSMOS_CONNECTION_STRING__', cosmosMongoConnectionString)
var appInit = replace(appInitWithConnection, '__COSMOS_DATABASE_NAME__', cosmosMongoDbName)

var keyVaultAdminRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '00482a5a-887f-4fb3-b363-3b7fe8e74483'
)

resource bastionAsg 'Microsoft.Network/applicationSecurityGroups@2024-05-01' = {
  name: bastionAsgName
  location: location
}

resource proxyAsg 'Microsoft.Network/applicationSecurityGroups@2024-05-01' = {
  name: proxyAsgName
  location: location
}

resource appAsg 'Microsoft.Network/applicationSecurityGroups@2024-05-01' = {
  name: appAsgName
  location: location
}

resource bastionNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: bastionNsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'Allow-SSH-From-Internet'
        properties: {
          priority: 100
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

resource proxyNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: proxyNsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'Allow-HTTP-From-Internet'
        properties: {
          priority: 100
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '80'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'Allow-SSH-From-Bastion'
        properties: {
          priority: 110
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceApplicationSecurityGroups: [
            {
              id: bastionAsg.id
            }
          ]
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

resource appNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: appNsgName
  location: location
  properties: {
    securityRules: [
      {
        name: 'Allow-App-From-Proxy'
        properties: {
          priority: 100
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '8080'
          sourceApplicationSecurityGroups: [
            {
              id: proxyAsg.id
            }
          ]
          destinationAddressPrefix: '*'
        }
      }
      {
        name: 'Allow-SSH-From-Bastion'
        properties: {
          priority: 110
          access: 'Allow'
          direction: 'Inbound'
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceApplicationSecurityGroups: [
            {
              id: bastionAsg.id
            }
          ]
          destinationAddressPrefix: '*'
        }
      }
    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: bastionSubnetName
        properties: {
          addressPrefix: bastionSubnetPrefix
          networkSecurityGroup: {
            id: bastionNsg.id
          }
        }
      }
      {
        name: proxySubnetName
        properties: {
          addressPrefix: proxySubnetPrefix
          networkSecurityGroup: {
            id: proxyNsg.id
          }
        }
      }
      {
        name: appSubnetName
        properties: {
          addressPrefix: appSubnetPrefix
          networkSecurityGroup: {
            id: appNsg.id
          }
        }
      }
    ]
  }
}

resource bastionPip 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: bastionPipName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
  }
}

resource proxyPip 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: proxyPipName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
  }
}

resource bastionNic 'Microsoft.Network/networkInterfaces@2024-05-01' = {
  name: bastionNicName
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          subnet: {
            id: '${vnet.id}/subnets/${bastionSubnetName}'
          }
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: bastionPip.id
          }
          applicationSecurityGroups: [
            {
              id: bastionAsg.id
            }
          ]
        }
      }
    ]
  }
}

resource proxyNic 'Microsoft.Network/networkInterfaces@2024-05-01' = {
  name: proxyNicName
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          subnet: {
            id: '${vnet.id}/subnets/${proxySubnetName}'
          }
          privateIPAllocationMethod: 'Dynamic'
          privateIPAddress: '10.0.1.4'
          publicIPAddress: {
            id: proxyPip.id
          }
          applicationSecurityGroups: [
            {
              id: proxyAsg.id
            }
          ]
        }
      }
    ]
  }
}

resource appNic 'Microsoft.Network/networkInterfaces@2024-05-01' = {
  name: appNicName
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        properties: {
          subnet: {
            id: '${vnet.id}/subnets/${appSubnetName}'
          }
          privateIPAllocationMethod: 'Static'
          privateIPAddress: '10.0.2.4'
          applicationSecurityGroups: [
            {
              id: appAsg.id
            }
          ]
        }
      }
    ]
  }
}

resource bastionVm 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: bastionVmName
  location: location
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    osProfile: {
      computerName: bastionVmName
      adminUsername: adminUsername
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${adminUsername}/.ssh/authorized_keys'
              keyData: sshPublicKey
            }
          ]
        }
      }
    }
    storageProfile: {
      imageReference: {
        publisher: imagePublisher
        offer: imageOffer
        sku: imageSku
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
        managedDisk: {
          storageAccountType: 'Standard_LRS'
        }
      }
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: bastionNic.id
        }
      ]
    }
  }
}

resource proxyVm 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: proxyVmName
  location: location
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    osProfile: {
      computerName: proxyVmName
      adminUsername: adminUsername
      customData: base64(loadTextContent('cloud-init-nginx.yaml'))
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${adminUsername}/.ssh/authorized_keys'
              keyData: sshPublicKey
            }
          ]
        }
      }
    }
    storageProfile: {
      imageReference: {
        publisher: imagePublisher
        offer: imageOffer
        sku: imageSku
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
        managedDisk: {
          storageAccountType: 'Standard_LRS'
        }
      }
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: proxyNic.id
        }
      ]
    }
  }
}

resource appVm 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: appVmName
  location: location
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    osProfile: {
      computerName: appVmName
      adminUsername: adminUsername
      customData: base64(appInit)
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${adminUsername}/.ssh/authorized_keys'
              keyData: sshPublicKey
            }
          ]
        }
      }
    }
    storageProfile: {
      imageReference: {
        publisher: imagePublisher
        offer: imageOffer
        sku: imageSku
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
        managedDisk: {
          storageAccountType: 'Standard_LRS'
        }
      }
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: appNic.id
        }
      ]
    }
  }
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2025-04-15' = {
  name: cosmosAccountName
  location: location
  kind: 'MongoDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    apiProperties: {
      serverVersion: '4.2'
    }
    capabilities: [
      {
        name: 'EnableMongo'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    publicNetworkAccess: 'Enabled'
    enableAutomaticFailover: false
    minimalTlsVersion: 'Tls12'
    disableLocalAuth: false
  }
}

resource cosmosMongoDb 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases@2025-04-15' = {
  parent: cosmosAccount
  name: cosmosMongoDbName
  properties: {
    resource: {
      id: cosmosMongoDbName
    }
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    publicNetworkAccess: 'Enabled'
    softDeleteRetentionInDays: 90
    enableSoftDelete: true
    enablePurgeProtection: true
  }
}

resource keyVaultAdminAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, currentUserObjectId, keyVaultAdminRoleDefinitionId)
  scope: keyVault
  properties: {
    principalId: currentUserObjectId
    roleDefinitionId: keyVaultAdminRoleDefinitionId
    principalType: 'User'
  }
}

output bastionPublicIp string = bastionPip.properties.ipAddress
output reverseProxyPublicIp string = proxyPip.properties.ipAddress
output appPrivateIp string = '10.0.2.4'
output reverseProxyUrl string = 'http://${proxyPip.properties.ipAddress}'
output cosmosEndpoint string = cosmosAccount.properties.documentEndpoint
output keyVaultNameOutput string = keyVault.name
output keyVaultId string = keyVault.id
output keyVaultUri string = keyVault.properties.vaultUri
