targetScope = 'resourceGroup'

@description('AZD environment name.')
param environmentName string

@description('Primary Azure region for all resources.')
param location string = resourceGroup().location

@description('Set to true for low-cost MVP development defaults.')
param useDevelopmentCostProfile bool = true

@secure()
@description('Shared SQL connection string used by StorageLens services.')
param sharedSqlConnectionString string

var resourceToken = uniqueString(subscription().id, resourceGroup().id, location, environmentName)
var tags = {
  'azd-env-name': environmentName
}

var appResourceNames = {
  web: 'azaca${resourceToken}01'
  locations: 'azaca${resourceToken}02'
  scanjobs: 'azaca${resourceToken}03'
  fileinventory: 'azaca${resourceToken}04'
  duplicates: 'azaca${resourceToken}05'
  analytics: 'azaca${resourceToken}06'
  scanner: 'azaca${resourceToken}07'
  hashing: 'azaca${resourceToken}08'
  orchestrator: 'azaca${resourceToken}09'
}

var internalServices = [
  'locations'
  'scanjobs'
  'fileinventory'
  'duplicates'
  'analytics'
  'scanner'
  'hashing'
  'orchestrator'
]

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'azlog${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: useDevelopmentCostProfile ? 7 : 30
    features: {
      searchVersion: 1
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-06-01-preview' = {
  name: 'azcr${resourceToken}'
  location: location
  sku: {
    name: useDevelopmentCostProfile ? 'Basic' : 'Standard'
  }
  tags: tags
  properties: {
    adminUserEnabled: false
    anonymousPullEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'azid${resourceToken}'
  location: location
  tags: tags
}

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' = {
  name: 'azcae${resourceToken}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    peerTrafficConfiguration: {
      encryption: {
        enabled: true
      }
    }
    zoneRedundant: false
  }
}

resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, userAssignedIdentity.id, 'acrpull')
  scope: containerRegistry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: userAssignedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

var fqdn = managedEnvironment.properties.defaultDomain

var endpoints = {
  locations: 'http://${appResourceNames.locations}.internal.${fqdn}'
  scanjobs: 'http://${appResourceNames.scanjobs}.internal.${fqdn}'
  fileinventory: 'http://${appResourceNames.fileinventory}.internal.${fqdn}'
  duplicates: 'http://${appResourceNames.duplicates}.internal.${fqdn}'
  analytics: 'http://${appResourceNames.analytics}.internal.${fqdn}'
  scanner: 'http://${appResourceNames.scanner}.internal.${fqdn}'
  hashing: 'http://${appResourceNames.hashing}.internal.${fqdn}'
  orchestrator: 'http://${appResourceNames.orchestrator}.internal.${fqdn}'
}

var defaultCors = {
  allowedOrigins: [
    '*'
  ]
  allowedMethods: [
    'GET'
    'POST'
    'PUT'
    'DELETE'
    'OPTIONS'
  ]
  allowedHeaders: [
    '*'
  ]
  exposeHeaders: [
    '*'
  ]
  allowCredentials: false
}

var commonHttpEnv = [
  {
    name: 'ASPNETCORE_URLS'
    value: 'http://+:8080'
  }
]

var envByService = {
  locations: concat(commonHttpEnv, [
    {
      name: 'ConnectionStrings__LocationsDb'
      value: sharedSqlConnectionString
    }
    {
      name: 'Database__UseInMemory'
      value: 'false'
    }
  ])
  scanjobs: concat(commonHttpEnv, [
    {
      name: 'ConnectionStrings__ScanJobsDb'
      value: sharedSqlConnectionString
    }
    {
      name: 'Database__UseInMemory'
      value: 'false'
    }
  ])
  fileinventory: concat(commonHttpEnv, [
    {
      name: 'ConnectionStrings__FileInventoryDb'
      value: sharedSqlConnectionString
    }
    {
      name: 'Database__UseInMemory'
      value: 'false'
    }
  ])
  duplicates: concat(commonHttpEnv, [
    {
      name: 'ConnectionStrings__DuplicatesDb'
      value: sharedSqlConnectionString
    }
    {
      name: 'Database__UseInMemory'
      value: 'false'
    }
    {
      name: 'UpstreamServices__FileInventory'
      value: endpoints.fileinventory
    }
  ])
  analytics: concat(commonHttpEnv, [
    {
      name: 'UpstreamServices__Locations'
      value: endpoints.locations
    }
    {
      name: 'UpstreamServices__ScanJobs'
      value: endpoints.scanjobs
    }
    {
      name: 'UpstreamServices__FileInventory'
      value: endpoints.fileinventory
    }
    {
      name: 'UpstreamServices__Duplicates'
      value: endpoints.duplicates
    }
  ])
  scanner: concat(commonHttpEnv, [
    {
      name: 'ConnectionStrings__ScannerDb'
      value: sharedSqlConnectionString
    }
    {
      name: 'Database__UseInMemory'
      value: 'false'
    }
    {
      name: 'UpstreamServices__FileInventory'
      value: endpoints.fileinventory
    }
    {
      name: 'UpstreamServices__ScanJobs'
      value: endpoints.scanjobs
    }
  ])
  hashing: concat(commonHttpEnv, [
    {
      name: 'ConnectionStrings__HashingDb'
      value: sharedSqlConnectionString
    }
    {
      name: 'Database__UseInMemory'
      value: 'false'
    }
    {
      name: 'UpstreamServices__FileInventory'
      value: endpoints.fileinventory
    }
    {
      name: 'UpstreamServices__ScanJobs'
      value: endpoints.scanjobs
    }
    {
      name: 'HashingWorker__MaxDegreeOfParallelism'
      value: '4'
    }
    {
      name: 'HashingWorker__BatchSize'
      value: '500'
    }
    {
      name: 'HashingWorker__MaxFileSizeBytes'
      value: '2147483648'
    }
  ])
  orchestrator: concat(commonHttpEnv, [
    {
      name: 'ConnectionStrings__OrchestratorDb'
      value: sharedSqlConnectionString
    }
    {
      name: 'Database__UseInMemory'
      value: 'false'
    }
    {
      name: 'UpstreamServices__Locations'
      value: endpoints.locations
    }
    {
      name: 'UpstreamServices__ScanJobs'
      value: endpoints.scanjobs
    }
    {
      name: 'UpstreamServices__Scanner'
      value: endpoints.scanner
    }
    {
      name: 'UpstreamServices__Hashing'
      value: endpoints.hashing
    }
    {
      name: 'UpstreamServices__Duplicates'
      value: endpoints.duplicates
    }
    {
      name: 'UpstreamServices__Analytics'
      value: endpoints.analytics
    }
  ])
}

resource webApp 'Microsoft.App/containerApps@2025-01-01' = {
  name: appResourceNames.web
  location: location
  tags: union(tags, {
    'azd-service-name': 'web'
  })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: managedEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: userAssignedIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        transport: 'auto'
        corsPolicy: defaultCors
      }
    }
    template: {
      containers: [
        {
          name: 'web'
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: {
            cpu: useDevelopmentCostProfile ? json('0.25') : json('0.5')
            memory: useDevelopmentCostProfile ? '0.5Gi' : '1Gi'
          }
          env: concat(commonHttpEnv, [
            {
              name: 'ServiceEndpoints__Locations'
              value: endpoints.locations
            }
            {
              name: 'ServiceEndpoints__ScanJobs'
              value: endpoints.scanjobs
            }
            {
              name: 'ServiceEndpoints__FileInventory'
              value: endpoints.fileinventory
            }
            {
              name: 'ServiceEndpoints__Duplicates'
              value: endpoints.duplicates
            }
            {
              name: 'ServiceEndpoints__Analytics'
              value: endpoints.analytics
            }
            {
              name: 'ServiceEndpoints__Orchestrator'
              value: endpoints.orchestrator
            }
          ])
        }
      ]
      scale: {
        minReplicas: useDevelopmentCostProfile ? 0 : 1
        maxReplicas: useDevelopmentCostProfile ? 1 : 3
      }
    }
  }
  dependsOn: [
    acrPullRoleAssignment
  ]
}

resource internalApps 'Microsoft.App/containerApps@2025-01-01' = [for service in internalServices: {
  name: appResourceNames[service]
  location: location
  tags: union(tags, {
    'azd-service-name': service
  })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: managedEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: userAssignedIdentity.id
        }
      ]
      ingress: {
        external: false
        targetPort: 8080
        allowInsecure: false
        transport: 'auto'
        corsPolicy: defaultCors
      }
    }
    template: {
      containers: [
        {
          name: service
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: {
            cpu: contains([
              'scanner'
              'hashing'
              'orchestrator'
            ], service)
              ? (useDevelopmentCostProfile ? json('0.25') : json('0.5'))
              : json('0.25')
            memory: contains([
              'scanner'
              'hashing'
              'orchestrator'
            ], service)
              ? (useDevelopmentCostProfile ? '0.5Gi' : '1Gi')
              : '0.5Gi'
          }
          env: envByService[service]
        }
      ]
      scale: {
        minReplicas: useDevelopmentCostProfile ? 0 : 1
        maxReplicas: useDevelopmentCostProfile ? 1 : 2
      }
    }
  }
  dependsOn: [
    acrPullRoleAssignment
  ]
}]

output RESOURCE_GROUP_ID string = resourceGroup().id
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = 'https://${containerRegistry.properties.loginServer}'
output WEB_FQDN string = webApp.properties.configuration.ingress.fqdn
