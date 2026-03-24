using './main.bicep'

param location = 'northeurope'
param prefix = 'demo'
param adminUsername = 'azureuser'
param vmSize = 'Standard_D2s_v3'

param sshPublicKey = 'ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIAp4jAHgrBAA+r9kcuW6HSaS9gE5mgdZiT5mVfxqxW16 stephenucheosedumme@Mac'

param cosmosAccountName = 'todoappcosmosmongo12345'
param cosmosMongoDbName = 'todoappdb'
