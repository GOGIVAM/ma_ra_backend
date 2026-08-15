# MA-RA Backend .NET

API métier du projet CAMRAIL MA-RA. Elle gère :

- l'authentification et les droits d'accès
- les utilisateurs et groupes CAMRAIL
- les équipements et gammes de maintenance
- les documents
- les KPI et journaux d'intervention
- le proxy vers le service IA Python

## Prérequis

- .NET SDK 8.0
- PostgreSQL 16 (local ou via Docker)
- Service IA Python démarré sur `http://localhost:8000`

## Configuration

Le fichier de configuration principal est :

- [appsettings.json](appsettings.json)

Pour le développement, les paramètres par défaut de l'application utilisent :

- une base PostgreSQL locale sur `localhost`
- un frontend sur `http://localhost:5173`
- un service FastAPI sur `http://localhost:8000`

## Démarrage local

Depuis le dossier du projet :

```bash
cd ma_ra_dotnet
dotnet restore
dotnet run --urls http://localhost:5080
```

Le backend démarrera sur :

- http://localhost:5080
- http://localhost:5080/swagger

## Base de données

Les migrations sont appliquées automatiquement au démarrage via le code de `Program.cs`.

Pour initialiser une base PostgreSQL locale, vous pouvez utiliser Docker :

```bash
docker run --name mara-postgres -e POSTGRES_DB=mara_db -e POSTGRES_USER=mara_user -e POSTGRES_PASSWORD=devpassword -p 5432:5432 -d postgres:16-alpine
```

Ensuite, vérifiez que la chaîne de connexion dans `appsettings.json` correspond à votre instance PostgreSQL.

## Variables d'environnement

Le projet lit les paramètres depuis `appsettings.json` et les variables de configuration ASP.NET Core. Les éléments clés sont :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mara_db;Username=mara_user;Password=devpassword"
  },
  "FastApi": {
    "BaseUrl": "http://localhost:8000"
  },
  "Jwt": {
    "SecretKey": "..."
  }
}
```

## Vérification rapide

```bash
curl http://localhost:5080/swagger
```

Si la page Swagger s'affiche, le backend est bien lancé.

## Développement avec la stack complète

Dans l'ordre recommandé :

1. Démarrer la base PostgreSQL
2. Démarrer le service Python IA
3. Démarrer l'API .NET
4. Démarrer le frontend React

## Dépannage

### L'API ne démarre pas

Vérifiez :

- la version .NET SDK : `dotnet --version`
- la validité de `appsettings.json`
- la présence de PostgreSQL et des accès de connexion
- la disponibilité du service FastAPI sur `http://localhost:8000`

### Le backend ne trouve pas le service IA

Vérifiez que le service Python est bien lancé :

```bash
curl http://localhost:8000/api/v1/health
```

### Accès refusé sur Swagger ou endpoints sécurisés

Le backend impose l'authentification JWT. Connectez-vous via l'endpoint d'authentification puis réessayez avec le token obtenu.

## Commandes utiles

```bash
dotnet restore
dotnet build
dotnet run --urls http://localhost:5080
```
