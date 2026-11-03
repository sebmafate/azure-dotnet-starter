# Starter .NET 10 · Azure

[Read in English](./README.en.md)

## À quoi ça sert

C'est un point de départ pour une API .NET qui publie des événements sur Azure Service Bus.
Je l'ai conçu pour montrer une architecture simple : le domaine ne dépend ni d'ASP.NET ni d'Azure,
l'infrastructure se décrit en Bicep, et aucun secret n'est stocké.

Le cas d'usage type : un service reçoit une commande, la valide, puis prévient les autres services
sans les appeler directement.

## Structure

| Dossier | Contenu |
|---|---|
| `src/Orders.Domain` | Règles métier et port `IOrderPublisher`. Aucune dépendance externe. |
| `src/Orders.Api` | API minimale : `POST /orders`, `GET /health`, et les deux éditeurs de messages. |
| `tests/Orders.Tests` | Tests unitaires du domaine et tests d'intégration de l'API. |
| `infra/main.bicep` | Service Bus, file `orders`, Log Analytics, Application Insights, identité managée. |
| `.forgejo/workflows/ci.yml` | Restauration, compilation, tests et validation Bicep. |

## Comment l'utiliser

### En local

```bash
dotnet test                          # 10 tests, sans Azure
dotnet run --project src/Orders.Api  # écoute sur http://localhost:5172
```

En local, `Orders:Publisher` vaut `InMemory` par défaut : les événements restent en mémoire.
Ils sont perdus au redémarrage, ce qui suffit pour tester.

```bash
curl -X POST http://localhost:5172/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":"c-1","lines":[{"sku":"A","quantity":2,"unitPrice":10}]}'
```

La réponse est `202 Accepted` avec l'identifiant et le total. Une commande invalide reçoit `400`.

### Sur Azure

```bash
az group create -n rg-orders-demo -l francecentral
az deployment group create -g rg-orders-demo -f infra/main.bicep -p prefix=ordersdemo
```

Le déploiement renvoie trois sorties : `serviceBusNamespace`, `queueName` et `identityClientId`.
Configurez ensuite l'API avec :

- `Orders:Publisher=ServiceBus`
- `ServiceBus:FullyQualifiedNamespace` = la valeur de `serviceBusNamespace`
- l'identité managée `identityClientId`, à associer à l'hébergement de l'API

L'API s'authentifie avec `DefaultAzureCredential`. Aucune chaîne de connexion n'est nécessaire.

### Ajouter une règle métier

Les règles sont dans `Order.Create` (`src/Orders.Domain/Order.cs`). Ajoutez le test correspondant dans
`tests/Orders.Tests/OrderTests.cs`, puis lancez `dotnet test`.

## Comment l'intégrer dans un projet existant

Trois options, de la plus légère à la plus complète :

1. **Le domaine seul** : copiez `src/Orders.Domain`. Il n'a aucune dépendance.
2. **Le port et l'éditeur** : reprenez `IOrderPublisher` et le dossier `src/Orders.Api/Publishing`.
   Vous pouvez garder votre propre API.
3. **L'infrastructure** : reprenez `infra/main.bicep` comme module dans votre propre déploiement.

```bicep
module orders 'chemin/vers/infra/main.bicep' = {
  name: 'orders'
  params: { prefix: 'monapp' }
}
```

## Limites

- Pas de base de données : le starter publie des événements, il ne les stocke pas.
- Pas d'OpenAPI ni de Swagger. Le paquet `Microsoft.OpenApi` 2.x a une vulnérabilité publiée
  (GHSA-v5pm-xwqc-g5wc), donc je l'ai retiré. Le fichier `Orders.Api.http` sert aux tests manuels.
- Pas d'OpenTelemetry explicite. Application Insights est provisionné, mais le code ne l'utilise pas encore.
- Le déploiement de l'API elle-même (App Service ou Container Apps) n'est pas décrit.
