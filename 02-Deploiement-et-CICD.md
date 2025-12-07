# 📘 DOCUMENTATION TECHNIQUE - PARTIE 2 : Déploiement et CI/CD
## Architecture Docker (Backend .NET 9 + PostgreSQL + NGINX) - Ubuntu 22.04
### VPS IP: 51.83.72.29 | Configuration Sécurisée

---

## 📋 TABLE DES MATIÈRES

1. [Déploiement initial](#1-déploiement-initial)
2. [Configuration GitHub Actions (CI/CD Automatique)](#2-configuration-github-actions-cicd-automatique)
3. [Scripts de maintenance et backup](#3-scripts-de-maintenance-et-backup)
4. [Monitoring et logs](#4-monitoring-et-logs)

---

## 1. DÉPLOIEMENT INITIAL

### 📌 ÉTAPE 1 : BUILDER ET DÉMARRER LES CONTAINERS

```bash
# Aller dans le répertoire principal
cd /srv/heptanet
```

**Explication** : Change de répertoire vers le dossier principal du projet où se trouve `docker-compose.yml`.

```bash
# Vérifier que le fichier .env existe et contient le mot de passe
cat .env
```

**Explication** : Affiche le contenu du fichier `.env` pour vérifier que le mot de passe PostgreSQL est défini. **Ne partagez jamais ce fichier !**

```bash
# Builder l'image du backend
docker compose build backend
```

**Explication** :
- `docker compose build` : Construit l'image Docker du service backend
- Utilise le `Dockerfile` dans le répertoire `./backend`
- Cette étape peut prendre plusieurs minutes la première fois

```bash
# Démarrer tous les services
docker compose up -d
```

**Explication** :
- `docker compose up` : Démarre tous les services définis dans `docker-compose.yml`
- `-d` : Mode détaché (en arrière-plan), permet de continuer à utiliser le terminal

```bash
# Vérifier que tous les containers sont en cours d'exécution
docker compose ps
```

**Explication** : Affiche l'état de tous les services. Vous devriez voir :
- `heptanet_postgres` : Up
- `heptanet_backend` : Up
- `heptanet_nginx` : Up

### 📌 ÉTAPE 2 : APPLIQUER LES MIGRATIONS DE LA BASE DE DONNÉES

```bash
# Attendre que PostgreSQL soit prêt
sleep 10
```

**Explication** : Attend 10 secondes pour laisser le temps à PostgreSQL de démarrer complètement.

```bash
# Exécuter les migrations depuis le container backend
docker compose exec backend dotnet ef database update \
  --project /src/HeptaNet.Infrastructure/HeptaNet.Infrastructure.csproj \
  --startup-project /src/HeptaNet.API/HeptaNet.API.csproj
```

**Explication** :
- `docker compose exec backend` : Exécute une commande dans le container backend
- `dotnet ef database update` : Applique les migrations Entity Framework à la base de données
- `--project` : Spécifie le projet contenant les migrations
- `--startup-project` : Spécifie le projet de démarrage

**Si dotnet-ef n'est pas disponible**, installez-le d'abord :

```bash
# Installer dotnet-ef dans le container
docker compose exec backend bash -c "dotnet tool install --global dotnet-ef && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef database update --project /src/HeptaNet.Infrastructure/HeptaNet.Infrastructure.csproj --startup-project /src/HeptaNet.API/HeptaNet.API.csproj"
```

**Explication** :
- `bash -c` : Exécute une commande bash dans le container
- Installe `dotnet-ef` globalement
- Met à jour le PATH pour inclure les outils .NET
- Exécute les migrations

**Alternative : Exécution manuelle** :

```bash
# Se connecter au container backend
docker compose exec backend bash
```

**Explication** : Ouvre un shell interactif dans le container backend.

```bash
# À l'intérieur du container
cd /src
dotnet tool install --global dotnet-ef
export PATH="$PATH:/root/.dotnet/tools"
dotnet ef database update --project HeptaNet.Infrastructure/HeptaNet.Infrastructure.csproj --startup-project HeptaNet.API/HeptaNet.API.csproj
exit
```

**Explication** :
- Change vers le répertoire source
- Installe l'outil Entity Framework
- Met à jour le PATH
- Applique les migrations
- Quitte le container

### 📌 ÉTAPE 3 : VÉRIFIER LES LOGS

```bash
# Voir les logs de tous les services
docker compose logs -f
```

**Explication** :
- `docker compose logs` : Affiche les logs de tous les services
- `-f` : Mode suivi (follow), affiche les nouveaux logs en temps réel
- Appuyez sur `Ctrl+C` pour quitter

```bash
# Voir les logs d'un service spécifique
docker compose logs -f backend
docker compose logs -f postgres
docker compose logs -f nginx
```

**Explication** : Affiche les logs d'un service spécifique. Utile pour déboguer un service en particulier.

```bash
# Voir les dernières lignes des logs
docker compose logs --tail=100 backend
```

**Explication** : `--tail=100` affiche uniquement les 100 dernières lignes des logs.

```bash
# Voir les logs depuis une date
docker compose logs --since 2024-01-01T00:00:00 backend
```

**Explication** : `--since` affiche les logs depuis une date/heure spécifique.

### 📌 ÉTAPE 4 : TESTER L'API

```bash
# Tester que l'API répond depuis le serveur
curl http://localhost/api/users
```

**Explication** :
- `curl` : Outil en ligne de commande pour faire des requêtes HTTP
- Teste l'endpoint `/api/users` via NGINX (port 80)

```bash
# Tester depuis l'extérieur (depuis votre machine locale)
curl http://51.83.72.29/api/users
```

**Explication** : Teste l'API depuis votre machine locale en utilisant l'IP publique du VPS.

```bash
# Tester Swagger (si activé)
curl http://51.83.72.29/swagger
```

**Explication** : Teste l'interface Swagger de l'API (documentation interactive).

```bash
# Tester avec plus de détails
curl -v http://51.83.72.29/api/users
```

**Explication** : `-v` (verbose) affiche les détails de la requête et de la réponse HTTP.

### 📌 ÉTAPE 5 : VÉRIFIER LA SANTÉ DES SERVICES

```bash
# Vérifier l'état des containers
docker compose ps
```

**Explication** : Affiche l'état de tous les services avec leurs ports et statuts.

```bash
# Vérifier les healthchecks
docker ps --format "table {{.Names}}\t{{.Status}}"
```

**Explication** :
- `docker ps` : Liste les conteneurs en cours d'exécution
- `--format` : Formate la sortie en tableau
- Affiche le nom et le statut de chaque conteneur

```bash
# Vérifier les ressources utilisées
docker stats --no-stream
```

**Explication** :
- `docker stats` : Affiche l'utilisation des ressources (CPU, RAM, réseau)
- `--no-stream` : Affiche une seule fois au lieu de mettre à jour en continu

✅ **Si tout fonctionne**, vous devriez voir :
- `heptanet_postgres` : Up (healthy)
- `heptanet_backend` : Up (healthy)
- `heptanet_nginx` : Up

---

## 2. CONFIGURATION GITHUB ACTIONS (CI/CD AUTOMATIQUE)

### 📌 OBJECTIF

Automatiser le déploiement : à chaque push ou merge sur la branche `main`, le code sera automatiquement déployé sur le VPS.

**Avantages** :
- Déploiement automatique sans intervention manuelle
- Réduction des erreurs humaines
- Historique des déploiements dans GitHub
- Tests automatiques avant déploiement (optionnel)

### 📌 ÉTAPE 1 : CRÉER LE RÉPERTOIRE .github/workflows

**Sur votre machine locale** (dans le repository GitHub) :

```bash
# Créer le répertoire
mkdir -p .github/workflows
```

**Explication** :
- `mkdir -p` : Crée le répertoire et tous les parents nécessaires
- `.github/workflows` : Emplacement standard pour les workflows GitHub Actions

### 📌 ÉTAPE 2 : CRÉER LE FICHIER DE WORKFLOW GITHUB ACTIONS

Créez le fichier `.github/workflows/deploy.yml` dans votre repository :

```yaml
name: Deploy to VPS

on:
  push:
    branches:
      - main
  workflow_dispatch: # Permet de déclencher manuellement

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Deploy to VPS
      uses: appleboy/ssh-action@v1.0.0
      with:
        host: ${{ secrets.VPS_HOST }}
        username: ${{ secrets.VPS_USERNAME }}
        key: ${{ secrets.VPS_SSH_KEY }}
        port: ${{ secrets.VPS_PORT }}
        script: |
          set -e  # Arrêter en cas d'erreur
          
          echo "🚀 Début du déploiement..."
          
          cd /srv/heptanet/backend
          
          # Sauvegarder l'état actuel
          git fetch origin
          CURRENT_COMMIT=$(git rev-parse HEAD)
          NEW_COMMIT=$(git rev-parse origin/main)
          
          if [ "$CURRENT_COMMIT" = "$NEW_COMMIT" ]; then
            echo "✅ Aucune nouvelle modification à déployer"
            exit 0
          fi
          
          echo "📥 Pull des dernières modifications..."
          git pull origin main
          
          echo "🔨 Build de l'image Docker..."
          cd /srv/heptanet
          docker compose build --no-cache backend
          
          echo "🔄 Redémarrage du service backend..."
          docker compose up -d --no-deps backend
          
          echo "⏳ Attente du démarrage du service..."
          sleep 10
          
          echo "📊 Vérification de l'état du service..."
          if docker compose ps backend | grep -q "Up"; then
            echo "✅ Service backend démarré avec succès"
          else
            echo "❌ Erreur : Le service backend n'a pas démarré"
            docker compose logs --tail=100 backend
            exit 1
          fi
          
          echo "🗄️ Application des migrations..."
          docker compose exec -T backend dotnet ef database update \
            --project /src/HeptaNet.Infrastructure/HeptaNet.Infrastructure.csproj \
            --startup-project /src/HeptaNet.API/HeptaNet.API.csproj || echo "⚠️ Aucune migration à appliquer"
          
          echo "📋 Logs récents du backend:"
          docker compose logs --tail=20 backend
          
          echo "🎉 Déploiement terminé avec succès !"
```

**Explication du workflow** :
- **`on: push: branches: - main`** : Déclenche le workflow à chaque push sur la branche main
- **`workflow_dispatch`** : Permet de déclencher manuellement depuis l'interface GitHub
- **`set -e`** : Arrête l'exécution si une commande échoue
- **Vérification des commits** : Évite de redéployer si rien n'a changé
- **`--no-cache`** : Force la reconstruction complète de l'image
- **`--no-deps`** : Redémarre uniquement le backend sans ses dépendances

### 📌 ÉTAPE 3 : CONFIGURER LES SECRETS GITHUB

Sur GitHub, allez dans votre repository :
1. **Settings** → **Secrets and variables** → **Actions**
2. Cliquez sur **"New repository secret"**
3. Ajoutez les secrets suivants :

**Secrets à créer** :

| Nom du Secret | Valeur | Description |
|--------------|--------|-------------|
| `VPS_HOST` | `51.83.72.29` | L'IP ou le domaine de votre VPS |
| `VPS_USERNAME` | `heptanet` | Le nom d'utilisateur SSH |
| `VPS_SSH_KEY` | `[votre clé privée SSH]` | La clé privée SSH pour se connecter |
| `VPS_PORT` | `22` | Le port SSH (ou votre port personnalisé) |

**Explication** :
- Les secrets sont chiffrés et ne sont jamais visibles dans les logs
- Utilisez `${{ secrets.NOM_SECRET }}` pour les référencer dans le workflow

### 📌 ÉTAPE 4 : GÉNÉRER UNE CLÉ SSH POUR GITHUB ACTIONS

**Sur votre VPS** :

```bash
# Générer une nouvelle clé SSH dédiée pour GitHub Actions
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github_actions_key -N ""
```

**Explication** :
- `-t ed25519` : Utilise l'algorithme Ed25519 (plus sécurisé)
- `-C` : Ajoute un commentaire
- `-f` : Spécifie le nom du fichier de clé
- `-N ""` : Pas de phrase de passe (nécessaire pour l'automatisation)

```bash
# Afficher la clé privée (à copier dans GitHub Secrets)
cat ~/.ssh/github_actions_key
```

**Explication** : Affiche le contenu de la clé privée. **Copiez TOUT le contenu** (y compris `-----BEGIN` et `-----END`) dans le secret `VPS_SSH_KEY` sur GitHub.

```bash
# Afficher la clé publique (à ajouter dans authorized_keys)
cat ~/.ssh/github_actions_key.pub
```

**Explication** : Affiche la clé publique à ajouter dans `authorized_keys`.

```bash
# Ajouter la clé publique à authorized_keys
cat ~/.ssh/github_actions_key.pub >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

**Explication** :
- `>>` : Ajoute la clé à la fin du fichier (sans écraser)
- `chmod 600` : Sécurise les permissions du fichier

### 📌 ÉTAPE 5 : CONFIGURER GIT SUR LE VPS POUR LE PULL AUTOMATIQUE

```bash
# Aller dans le répertoire backend
cd /srv/heptanet/backend
```

**Explication** : Change vers le répertoire où se trouve le code source.

**Option 1 : Utiliser HTTPS avec un token GitHub** :

```bash
# Créer un Personal Access Token sur GitHub
# Settings → Developer settings → Personal access tokens → Generate new token
# Permissions : repo (full control)

# Configurer Git avec le token
git remote set-url origin https://VOTRE_TOKEN@github.com/VOTRE_USERNAME/heptanet-backend.git
```

**Explication** :
- Créez un token avec les permissions `repo`
- Remplacez `VOTRE_TOKEN` et `VOTRE_USERNAME` par vos valeurs
- Le token sera utilisé pour l'authentification

**Option 2 : Utiliser SSH (recommandé)** :

```bash
# Créer une clé SSH dédiée pour le serveur
ssh-keygen -t ed25519 -C "vps-deploy" -f ~/.ssh/vps_deploy_key -N ""
```

**Explication** : Génère une nouvelle paire de clés SSH pour le déploiement.

```bash
# Afficher la clé publique
cat ~/.ssh/vps_deploy_key.pub
```

**Explication** : Affiche la clé publique à ajouter sur GitHub.

**Sur GitHub** :
1. Allez dans votre repository
2. **Settings** → **Deploy keys**
3. Cliquez sur **"Add deploy key"**
4. Collez la clé publique
5. Cochez **"Allow write access"** (si vous voulez que le serveur puisse push)

```bash
# Configurer Git pour utiliser SSH
git remote set-url origin git@github.com:VOTRE_USERNAME/heptanet-backend.git
```

**Explication** : Configure Git pour utiliser SSH au lieu de HTTPS.

```bash
# Tester la connexion
git fetch origin
```

**Explication** : Teste que Git peut se connecter au repository GitHub.

### 📌 ÉTAPE 6 : TESTER LE DÉPLOIEMENT AUTOMATIQUE

**Sur votre machine locale** :

```bash
# Faire une modification dans votre code
# Par exemple, ajouter un commentaire dans Program.cs

# Commiter les changements
git add .
git commit -m "Test deployment automation"
```

**Explication** :
- `git add .` : Ajoute tous les fichiers modifiés
- `git commit` : Crée un commit avec un message

```bash
# Pousser sur la branche main
git push origin main
```

**Explication** : Pousse les changements vers GitHub, ce qui déclenchera automatiquement le workflow.

**Sur GitHub** :
1. Allez dans votre repository
2. Cliquez sur l'onglet **"Actions"**
3. Vous devriez voir le workflow **"Deploy to VPS"** en cours d'exécution
4. Cliquez dessus pour voir les logs en temps réel

**Vérification sur le VPS** :

```bash
# Vérifier que le code a été mis à jour
cd /srv/heptanet/backend
git log -1
```

**Explication** : Affiche le dernier commit pour vérifier que le code a été mis à jour.

```bash
# Vérifier que le backend a été redémarré
docker compose ps backend
docker compose logs --tail=20 backend
```

**Explication** : Vérifie que le service backend fonctionne avec la nouvelle version.

---

## 3. SCRIPTS DE MAINTENANCE ET BACKUP

### 📌 ÉTAPE 1 : CRÉER UN SCRIPT DE BACKUP POSTGRESQL

```bash
cd /srv/heptanet
nano backup-db.sh
```

**Explication** : Crée un script shell pour automatiser les sauvegardes de la base de données.

**Collez ce contenu** :

```bash
#!/bin/bash

# Configuration
BACKUP_DIR="/srv/heptanet/backups"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/heptanet_db_$DATE.sql"
RETENTION_DAYS=7
LOG_FILE="/srv/heptanet/logs/backup.log"

# Créer les répertoires si nécessaire
mkdir -p $BACKUP_DIR
mkdir -p $(dirname $LOG_FILE)

# Fonction de logging
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a $LOG_FILE
}

log "🔄 Début du backup de la base de données..."

# Faire le backup
if docker compose exec -T postgres pg_dump -U heptanet_user heptanet_db > $BACKUP_FILE 2>>$LOG_FILE; then
    # Compresser le backup
    gzip $BACKUP_FILE
    BACKUP_FILE_GZ="$BACKUP_FILE.gz"
    
    # Vérifier la taille du backup
    BACKUP_SIZE=$(du -h $BACKUP_FILE_GZ | cut -f1)
    log "✅ Backup créé : $BACKUP_FILE_GZ (Taille: $BACKUP_SIZE)"
    
    # Supprimer les anciens backups (plus de 7 jours)
    log "🧹 Nettoyage des anciens backups..."
    find $BACKUP_DIR -name "heptanet_db_*.sql.gz" -mtime +$RETENTION_DAYS -delete
    
    log "✅ Backup terminé avec succès"
    exit 0
else
    log "❌ Erreur lors de la création du backup"
    exit 1
fi
```

**Explication du script** :
- **Variables** : Définit les chemins et noms de fichiers
- **`date +%Y%m%d_%H%M%S`** : Génère un timestamp pour le nom du fichier
- **`pg_dump`** : Exporte la base de données PostgreSQL
- **`gzip`** : Compresse le backup pour économiser l'espace
- **`find ... -mtime +7 -delete`** : Supprime les backups de plus de 7 jours
- **Logging** : Enregistre toutes les opérations dans un fichier de log

```bash
# Rendre le script exécutable
chmod +x backup-db.sh
```

**Explication** : `chmod +x` donne la permission d'exécution au script.

```bash
# Tester le script manuellement
./backup-db.sh
```

**Explication** : Exécute le script pour tester qu'il fonctionne correctement.

### 📌 ÉTAPE 2 : AUTOMATISER LES BACKUPS

```bash
# Éditer le crontab
crontab -e
```

**Explication** : Ouvre l'éditeur de crontab pour planifier des tâches automatiques.

**Ajoutez cette ligne** :

```cron
# Backup quotidien à 2h du matin
0 2 * * * /srv/heptanet/backup-db.sh >> /srv/heptanet/logs/backup-cron.log 2>&1
```

**Explication de la syntaxe cron** :
- `0 2 * * *` : À 2h00 du matin, tous les jours
- Format : `minute heure jour mois jour-semaine`
- `>>` : Ajoute la sortie à la fin du fichier (append)
- `2>&1` : Redirige les erreurs vers la sortie standard

**Autres exemples de planification** :

```cron
# Toutes les 6 heures
0 */6 * * * /srv/heptanet/backup-db.sh

# Tous les lundis à 3h du matin
0 3 * * 1 /srv/heptanet/backup-db.sh

# Tous les 1er du mois à minuit
0 0 1 * * /srv/heptanet/backup-db.sh
```

```bash
# Vérifier les tâches cron configurées
crontab -l
```

**Explication** : Affiche toutes les tâches cron configurées pour l'utilisateur actuel.

### 📌 ÉTAPE 3 : CRÉER UN SCRIPT DE RESTAURATION

```bash
cd /srv/heptanet
nano restore-db.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

if [ -z "$1" ]; then
    echo "Usage: ./restore-db.sh <backup_file.sql.gz>"
    echo "Exemple: ./restore-db.sh /srv/heptanet/backups/heptanet_db_20240101_020000.sql.gz"
    exit 1
fi

BACKUP_FILE=$1

if [ ! -f "$BACKUP_FILE" ]; then
    echo "❌ Erreur : Le fichier $BACKUP_FILE n'existe pas"
    exit 1
fi

echo "⚠️  ATTENTION : Cette opération va écraser la base de données actuelle !"
read -p "Êtes-vous sûr ? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "❌ Opération annulée"
    exit 0
fi

echo "🔄 Restauration de la base de données..."

# Décompresser si nécessaire
if [[ $BACKUP_FILE == *.gz ]]; then
    gunzip -c $BACKUP_FILE | docker compose exec -T postgres psql -U heptanet_user -d heptanet_db
else
    cat $BACKUP_FILE | docker compose exec -T postgres psql -U heptanet_user -d heptanet_db
fi

if [ $? -eq 0 ]; then
    echo "✅ Restauration terminée avec succès"
else
    echo "❌ Erreur lors de la restauration"
    exit 1
fi
```

**Explication du script** :
- **Vérification des arguments** : Vérifie qu'un fichier de backup est fourni
- **Vérification de l'existence** : Vérifie que le fichier existe
- **Confirmation** : Demande confirmation avant de restaurer
- **Décompression** : Décompresse automatiquement si le fichier est compressé
- **Restauration** : Importe le backup dans PostgreSQL

```bash
# Rendre exécutable
chmod +x restore-db.sh
```

**Utilisation** :

```bash
# Lister les backups disponibles
ls -lh /srv/heptanet/backups/

# Restaurer un backup
./restore-db.sh /srv/heptanet/backups/heptanet_db_20240101_020000.sql.gz
```

### 📌 ÉTAPE 4 : CRÉER UN SCRIPT DE MAINTENANCE GÉNÉRAL

```bash
cd /srv/heptanet
nano maintenance.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

echo "🔧 Maintenance du système HeptaNet"
echo "===================================="
echo ""

# Nettoyer les images Docker inutilisées
echo "🧹 Nettoyage des images Docker..."
docker system prune -f --volumes

# Vérifier l'espace disque
echo ""
echo "💾 Espace disque disponible:"
df -h /srv

# Vérifier l'état des containers
echo ""
echo "📊 État des containers:"
docker compose ps

# Vérifier les logs d'erreur récents
echo ""
echo "📋 Logs d'erreur récents:"
docker compose logs --tail=50 | grep -i error || echo "Aucune erreur récente"

# Vérifier les ressources
echo ""
echo "📈 Utilisation des ressources:"
docker stats --no-stream

# Vérifier les backups
echo ""
echo "💾 Derniers backups:"
ls -lh /srv/heptanet/backups/ | tail -5

echo ""
echo "✅ Maintenance terminée"
```

**Explication du script** :
- **`docker system prune -f`** : Supprime les images, conteneurs et volumes inutilisés
- **`df -h`** : Affiche l'espace disque disponible
- **`docker stats`** : Affiche l'utilisation des ressources
- **`grep -i error`** : Recherche les erreurs dans les logs

```bash
# Rendre exécutable
chmod +x maintenance.sh
```

**Utilisation** :

```bash
# Exécuter la maintenance
./maintenance.sh
```

### 📌 ÉTAPE 5 : CRÉER UN SCRIPT DE MONITORING

```bash
cd /srv/heptanet
nano monitor.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

echo "📊 État du système HeptaNet"
echo "============================"
echo ""

echo "🐳 Containers:"
docker compose ps
echo ""

echo "💾 Utilisation des ressources:"
docker stats --no-stream
echo ""

echo "📁 Espace disque:"
df -h /srv
echo ""

echo "📋 Logs d'erreur récents (backend):"
docker compose logs --tail=20 backend | grep -i error || echo "Aucune erreur"
echo ""

echo "🔗 Santé des services:"
curl -s http://localhost:5106/health || echo "Backend non accessible"
echo ""

echo "📊 Connexions actives (PostgreSQL):"
docker compose exec -T postgres psql -U heptanet_user -d heptanet_db -c "SELECT count(*) FROM pg_stat_activity WHERE state = 'active';" 2>/dev/null || echo "Impossible de récupérer les statistiques"
```

**Explication du script** :
- Affiche l'état de tous les conteneurs
- Montre l'utilisation des ressources
- Vérifie l'espace disque
- Recherche les erreurs dans les logs
- Teste la santé de l'API
- Affiche les statistiques PostgreSQL

```bash
# Rendre exécutable
chmod +x monitor.sh
```

**Utilisation** :

```bash
# Exécuter le monitoring
./monitor.sh
```

---

## 4. MONITORING ET LOGS

### 📌 ÉTAPE 1 : CONSULTER LES LOGS

```bash
# Logs de tous les services
cd /srv/heptanet
docker compose logs -f
```

**Explication** : Affiche les logs de tous les services en temps réel.

```bash
# Logs d'un service spécifique
docker compose logs -f backend
docker compose logs -f postgres
docker compose logs -f nginx
```

**Explication** : Affiche les logs d'un service spécifique.

```bash
# Logs des 100 dernières lignes
docker compose logs --tail=100 backend
```

**Explication** : Affiche uniquement les 100 dernières lignes.

```bash
# Logs depuis une date
docker compose logs --since 2024-01-01T00:00:00 backend
```

**Explication** : Affiche les logs depuis une date/heure spécifique.

```bash
# Logs avec horodatage
docker compose logs -t backend
```

**Explication** : `-t` ajoute un horodatage à chaque ligne de log.

### 📌 ÉTAPE 2 : MONITORER LES RESSOURCES

```bash
# Utilisation des ressources par container
docker stats
```

**Explication** : Affiche l'utilisation des ressources (CPU, RAM, réseau) en temps réel pour tous les conteneurs.

```bash
# Utilisation des ressources (une seule fois)
docker stats --no-stream
```

**Explication** : Affiche l'utilisation des ressources une seule fois (pas de mise à jour continue).

```bash
# Espace disque utilisé par les volumes
docker system df -v
```

**Explication** :
- `docker system df` : Affiche l'espace disque utilisé par Docker
- `-v` : Mode verbeux (détails par volume)

```bash
# Informations sur les containers
docker compose ps
docker inspect heptanet_backend
```

**Explication** :
- `docker compose ps` : Liste les services avec leur état
- `docker inspect` : Affiche toutes les informations détaillées d'un conteneur

### 📌 ÉTAPE 3 : CONFIGURER LA ROTATION DES LOGS

```bash
# Créer un fichier de configuration logrotate pour Docker
sudo nano /etc/logrotate.d/docker-containers
```

**Collez ce contenu** :

```
/srv/heptanet/logs/*.log {
    daily
    rotate 7
    compress
    delaycompress
    notifempty
    missingok
}
```

**Explication** :
- **`daily`** : Rotation quotidienne
- **`rotate 7`** : Garde 7 fichiers de log archivés
- **`compress`** : Compresse les anciens logs
- **`delaycompress`** : Retarde la compression d'un jour
- **`notifempty`** : Ne fait rien si le fichier est vide
- **`missingok`** : Ne génère pas d'erreur si le fichier est absent

```bash
# Tester la configuration
sudo logrotate -d /etc/logrotate.d/docker-containers
```

**Explication** : `-d` teste la configuration en mode debug sans appliquer les changements.

### 📌 ÉTAPE 4 : CRÉER UN SCRIPT DE SURVEILLANCE AUTOMATIQUE

```bash
cd /srv/heptanet
nano health-check.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

LOG_FILE="/srv/heptanet/logs/health-check.log"
DATE=$(date '+%Y-%m-%d %H:%M:%S')

# Fonction de logging
log() {
    echo "[$DATE] $1" | tee -a $LOG_FILE
}

# Vérifier que les containers sont en cours d'exécution
if ! docker compose ps | grep -q "Up"; then
    log "❌ ALERTE : Un ou plusieurs containers ne sont pas en cours d'exécution"
    docker compose ps >> $LOG_FILE
    # Ici vous pourriez envoyer une alerte par email
fi

# Vérifier l'espace disque
DISK_USAGE=$(df -h /srv | awk 'NR==2 {print $5}' | sed 's/%//')
if [ $DISK_USAGE -gt 80 ]; then
    log "⚠️  ALERTE : Espace disque utilisé à ${DISK_USAGE}%"
fi

# Vérifier la santé de l'API
if ! curl -s http://localhost:5106/health > /dev/null; then
    log "❌ ALERTE : L'API ne répond pas"
fi

log "✅ Vérification de santé terminée"
```

**Explication du script** :
- Vérifie que tous les conteneurs sont en cours d'exécution
- Vérifie l'espace disque (alerte si > 80%)
- Vérifie que l'API répond
- Enregistre tout dans un fichier de log

```bash
# Rendre exécutable
chmod +x health-check.sh
```

**Ajouter au crontab** :

```bash
crontab -e
```

**Ajoutez** :

```cron
# Vérification de santé toutes les 5 minutes
*/5 * * * * /srv/heptanet/health-check.sh
```

---

## ✅ CHECKLIST - PARTIE 2

- [ ] Déploiement initial réussi
- [ ] Migrations de base de données appliquées
- [ ] API accessible depuis l'extérieur
- [ ] GitHub Actions configuré
- [ ] Secrets GitHub configurés
- [ ] Clé SSH pour GitHub Actions générée
- [ ] Git configuré sur le VPS
- [ ] Déploiement automatique testé
- [ ] Script de backup créé et testé
- [ ] Backup automatique configuré (cron)
- [ ] Script de restauration créé
- [ ] Script de maintenance créé
- [ ] Script de monitoring créé
- [ ] Rotation des logs configurée

---

**📌 Suite dans le fichier suivant : `03-Securite-et-Monitoring.md`**

