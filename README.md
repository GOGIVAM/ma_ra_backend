# MA-RA Backend .NET

API métier du projet CAMRAIL MA-RA. Elle gère :

- l'authentification et les droits d'accès (JWT, groupes CAMRAIL)
- les utilisateurs, équipements, gammes de maintenance, documents
- les KPI et journaux d'intervention
- le proxy vers le service IA Python

## Prérequis

- .NET SDK 8.0
- PostgreSQL 16 (local ou via Docker)
- Service IA Python démarré sur `http://localhost:8000` (optionnel  uniquement pour les routes d'inférence)

## Configuration

Deux fichiers de configuration coexistent :

| Fichier | Utilisé quand |
|---|---|
| `appsettings.Development.json` | `ASPNETCORE_ENVIRONMENT=Development` (dev local) |
| `appsettings.json` | Production (défaut si variable absente) |

**En développement**, la connexion cible `mara_dev` avec l'utilisateur `mara_user`.

## Préparation de la base de données (première fois)

PostgreSQL doit être démarré. Créer le rôle et la base avec un superutilisateur :

```powershell
$env:PGPASSWORD = "<votre_mdp_superuser>"
psql -U <superuser> -h localhost -d postgres -c "CREATE USER mara_user WITH PASSWORD 'devpassword';"
psql -U <superuser> -h localhost -d postgres -c "CREATE DATABASE mara_dev OWNER mara_user;"
psql -U <superuser> -h localhost -d postgres -c "GRANT ALL PRIVILEGES ON DATABASE mara_dev TO mara_user;"
```

Les tables sont créées automatiquement au premier démarrage (migrations EF Core).

## Démarrage local

```powershell
cd ma_ra_dotnet
dotnet restore
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls http://localhost:5080
```

> **Important** : `ASPNETCORE_ENVIRONMENT=Development` est obligatoire. Sans lui, le backend démarre en mode Production : Swagger désactivé, seed ignoré, et la connexion pointe vers la base de prod définie dans `appsettings.json`.

Le backend est accessible sur :

- http://localhost:5080/swagger (Swagger UI)
- http://localhost:5080/api/health

## Seed automatique

Au démarrage en mode `Development`, le backend crée automatiquement :

- **7 utilisateurs** de test (voir tableau ci-dessous)
- **12 équipements** CAMRAIL (8 DMAT + 4 DIF)
- **14 gammes de maintenance** avec leurs étapes

Le seed est idempotent : il ne recrée pas les données si elles existent déjà.

## Comptes de test

| Utilisateur | Mot de passe | Groupe | Droits |
|---|---|---|---|
| `admin` | `Admin@Camrail2025!` | ADMIN | Accès total |
| `tech.dmat` | `Tech@Dmat2025!` | DMAT | Équipements + gammes DMAT |
| `chef.dmat` | `Chef@Dmat2025!` | DMAT | Équipements + gammes DMAT |
| `tech.dif` | `Tech@Dif2025!` | DIF | Équipements + gammes DIF |
| `chef.dif` | `Chef@Dif2025!` | DIF | Équipements + gammes DIF |
| `coordinateur` | `Coord@CI2025!` | CI | Lecture + création gammes |
| `formateur` | `Form@CIF2025!` | CIF | Lecture + création gammes |

Obtenir un token JWT :

```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@Camrail2025!"}'
```

Le token retourné s'utilise dans le header `Authorization: Bearer <token>`.

## Migrations EF Core

Les migrations sont appliquées automatiquement au démarrage. Pour les gérer manuellement :

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
# Créer une nouvelle migration
dotnet ef migrations add NomDeLaMigration
# Appliquer manuellement
dotnet ef database update
# Annuler la dernière migration (base vide uniquement)
dotnet ef migrations remove
```

## Dépannage

### Erreur "address already in use" sur le port 5080

Un process précédent tourne encore. Pour libérer le port :

```powershell
$pid = (netstat -ano | Select-String ":5080 ").ToString().Trim().Split()[-1]
Stop-Process -Id $pid -Force
```

### Swagger non disponible

Vérifiez que `ASPNETCORE_ENVIRONMENT=Development` est bien défini. Swagger est désactivé en Production.

### Erreur "AspNetUsers n'existe pas" au démarrage

Les migrations n'ont pas été générées. Exécutez :

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef migrations add InitialCreate
dotnet run --urls http://localhost:5080
```

### Le backend ne trouve pas le service IA

Le service Python est optionnel pour les routes non-IA. Vérifiez qu'il tourne :

```bash
curl http://localhost:8000/api/v1/health
```

## Commandes utiles

```powershell
dotnet restore
dotnet build
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls http://localhost:5080
```
