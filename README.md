# Starter .NET 10 · Azure

Modèle de référence pour une API .NET qui publie des événements sur Azure Service Bus, avec tests,
pipeline CI et infrastructure en Bicep. Il illustre une architecture simple, découplée et sans secret.

## Ce qu'il montre

- **Domaine pur** (`src/Orders.Domain`) : aucune dépendance à ASP.NET ni à Azure. Les règles métier
  (au moins une ligne, quantité positive, prix non négatif) vivent dans `Order.Create`.
- **Port de publication** : `IOrderPublisher` est implémenté par Service Bus en production et en mémoire
  en local. Le domaine ne connaît pas Azure.
- **API minimale** (`src/Orders.Api`) : `POST /orders` renvoie `202 Accepted` et publie un événement
  `OrderPlaced` ; `GET /health` pour les sondes de disponibilité.
- **Authentification sans secret** : `DefaultAzureCredential` et identité managée, avec `disableLocalAuth`
  côté Service Bus (aucune clé SAS).
- **Infrastructure as code** (`infra/main.bicep`) : espace de noms Service Bus, file `orders`
  (dead-lettering activé), Log Analytics, Application Insights, identité managée et rôle RBAC d'expéditeur.
- **Tests** (`tests/Orders.Tests`) : tests unitaires du domaine et tests d'intégration de l'API
  avec `WebApplicationFactory`.
- **CI** (`.forgejo/workflows/ci.yml`) : restauration, compilation avec avertissements bloquants,
  tests, validation Bicep.

## Lancer en local

```bash
dotnet test                        # tests (publication en mémoire)
dotnet run --project src/Orders.Api
curl -X POST http://localhost:5172/orders -H "Content-Type: application/json" \
  -d '{"customerId":"c-1","lines":[{"sku":"A","quantity":2,"unitPrice":10}]}'
```

## Déployer sur Azure

```bash
az group create -n rg-orders-demo -l francecentral
az deployment group create -g rg-orders-demo -f infra/main.bicep -p prefix=ordersdemo
```

Puis configurer l'API avec `Orders:Publisher=ServiceBus`, `ServiceBus:FullyQualifiedNamespace`
(sortie `serviceBusNamespace`) et l'identité managée (sortie `identityClientId`).

## Choix assumés et limites

- Pas de base de données : le starter se concentre sur la publication d'événements.
- Pas d'OpenAPI ni de Swagger : le paquet `Microsoft.OpenApi` 2.x porte une vulnérabilité publiée
  (GHSA-v5pm-xwqc-g5wc) ; il est retiré. Un `.http` reste disponible pour les tests manuels.
- Pas d'Aspire ni d'OpenTelemetry explicite : pistes d'évolution. Application Insights est provisionné.
- Le déploiement applicatif (App Service ou Container Apps) n'est pas décrit ici.
