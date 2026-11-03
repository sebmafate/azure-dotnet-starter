// Ressources Azure du starter : espace de noms Service Bus, file « orders »,
// journalisation (Log Analytics + Application Insights) et identité managée pour l'API.
// Authentification par identité (RBAC), sans chaîne de connexion ni clé d'accès.

@description('Région de déploiement.')
param location string = resourceGroup().location

@description('Préfixe court pour nommer les ressources (lettres minuscules et chiffres).')
@minLength(3)
@maxLength(12)
param prefix string

@description('Nom de la file de commandes.')
param queueName string = 'orders'

var suffix = uniqueString(resourceGroup().id)
var namespaceName = '${prefix}-sb-${suffix}'

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-log-${suffix}'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-appi-${suffix}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
  }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${prefix}-id-${suffix}'
  location: location
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    disableLocalAuth: true // aucune clé SAS : identité uniquement
    minimumTlsVersion: '1.2'
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBus
  name: queueName
  properties: {
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
    lockDuration: 'PT1M'
  }
}

// Rôle « Expéditeur de données Azure Service Bus » pour l'identité de l'API.
var senderRoleId = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'

resource sendRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBus.id, identity.id, senderRoleId)
  scope: queue
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', senderRoleId)
  }
}

output serviceBusNamespace string = '${namespaceName}.servicebus.windows.net'
output queueName string = queueName
output identityClientId string = identity.properties.clientId
output appInsightsConnectionString string = insights.properties.ConnectionString
