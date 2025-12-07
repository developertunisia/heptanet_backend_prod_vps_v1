# 📘 DOCUMENTATION TECHNIQUE - PARTIE 1 : Installation et Configuration
## Architecture Docker (Backend .NET 9 + PostgreSQL + NGINX) - Ubuntu 22.04
### VPS IP: 51.83.72.29 | Configuration Sécurisée

---

## 📋 TABLE DES MATIÈRES

1. [Vue d'ensemble et prérequis](#1-vue-densemble-et-prérequis)
2. [Caractéristiques du serveur recommandées](#2-caractéristiques-du-serveur-recommandées)
3. [Architecture globale](#3-architecture-globale)
4. [Sécurité initiale du serveur (CRITIQUE)](#4-sécurité-initiale-du-serveur-critique)
5. [Connexion SSH au VPS](#5-connexion-ssh-au-vps)
6. [Installation de Docker et Docker Compose](#6-installation-de-docker-et-docker-compose)
7. [Configuration de l'environnement](#7-configuration-de-lenvironnement)
8. [Structure du projet sur le serveur](#8-structure-du-projet-sur-le-serveur)
9. [Configuration du backend .NET 9](#9-configuration-du-backend-net-9)
10. [Configuration PostgreSQL (sécurisée)](#10-configuration-postgresql-sécurisée)
11. [Configuration NGINX (Reverse Proxy - IP uniquement)](#11-configuration-nginx-reverse-proxy---ip-uniquement)
12. [Configuration Docker Compose](#12-configuration-docker-compose)
13. [Configuration du Firewall (UFW)](#13-configuration-du-firewall-ufw)

---

## 1. VUE D'ENSEMBLE ET PRÉREQUIS

### 📌 CONTEXTE DU PROJET

Notre solution backend HeptaNet est une API .NET 9.0 développée avec une architecture en couches :
- **HeptaNet.API** : Contrôleurs, Hubs SignalR, Middleware
- **HeptaNet.Application** : Services métier
- **HeptaNet.Domain** : Modèles, DTOs, Interfaces
- **HeptaNet.Infrastructure** : Base de données, Repositories, Email, File Storage

Le code source est versionné dans un dépôt GitHub.

**Configuration actuelle :**
- VPS IP : `51.83.72.29`
- Pas de nom de domaine (configuration IP uniquement)
- Backend accessible via HTTP (port 80 via NGINX)
- URL API Frontend : `http://51.83.72.29`

### 📌 PRÉREQUIS

- Un VPS Ubuntu 22.04 LTS avec accès root ou sudo
- Accès SSH au serveur (IP: 51.83.72.29)
- Un compte GitHub avec le repository du projet
- Connaissance basique de Linux et Docker

---

## 2. CARACTÉRISTIQUES DU SERVEUR RECOMMANDÉES

### 💻 SPÉCIFICATIONS MINIMALES RECOMMANDÉES

- **CPU** : 4 vCPU (minimum 2 vCPU)
- **RAM** : 8 GB (minimum 4 GB)
- **Stockage** : 50 GB NVMe SSD (minimum 20 GB)
- **OS** : Ubuntu 22.04 LTS
- **IP Publique** : 51.83.72.29

### 💡 POURQUOI CES SPECS ?

- **Docker** : Consomme CPU + RAM pour les containers
- **PostgreSQL** : Très gourmand en RAM (recommandé 4-8 GB pour production)
- **Logs + volumes Docker** : Nécessitent de l'espace disque
- **Charge prévue** : Scalable jusqu'à 50k utilisateurs simultanés
- **SignalR** : Consomme de la RAM pour les connexions WebSocket

---

## 3. ARCHITECTURE GLOBALE

```
                    INTERNET
                       │
                    HTTP 80
                       │
              ┌────────▼─────────┐
              │      NGINX       │  ← Reverse Proxy
              │   (container)    │     Port: 80 (HTTP)
              │  IP: 51.83.72.29 │     Pas de SSL (IP uniquement)
              └────────┬─────────┘
                       │
       ┌───────────────┼────────────────┐
       │                               │
┌──────▼──────────┐          ┌──────────▼──────────┐
│ Backend .NET 9   │          │ PostgreSQL 16       │
│ (container)      │          │ (container)         │
│ Port: 5106       │◄─────────┤ Port: 5432          │
└──────────────────┘          └─────────────────────┘
```

### 📌 EXPLICATION DE L'ARCHITECTURE

1. **NGINX** : Reçoit toutes les requêtes HTTP depuis Internet (port 80)
2. **NGINX → Backend** : Redirige les requêtes vers le container backend sur le port 5106
3. **Backend → PostgreSQL** : Se connecte à PostgreSQL via le réseau Docker interne
4. **Tous les services** : Communiquent via le réseau Docker (isolé et sécurisé)
5. **Pas de SSL/HTTPS** : Car utilisation d'une IP uniquement (pas de domaine)

---

## 4. SÉCURITÉ INITIALE DU SERVEUR (CRITIQUE)

🔒 **Cette section est CRITIQUE pour la sécurité de votre serveur. Ne la sautez pas !**

### 📌 ÉTAPE 1 : MISE À JOUR DU SYSTÈME

```bash
# Se connecter au serveur
ssh root@51.83.72.29
```

**Explication** : Connexion SSH au serveur avec l'utilisateur root. Remplacez `root` par votre utilisateur si vous en avez un.

```bash
# Mettre à jour la liste des paquets
sudo apt update
```

**Explication** : `apt update` met à jour la liste des paquets disponibles depuis les dépôts Ubuntu. `sudo` donne les privilèges administrateur nécessaires.

```bash
# Mettre à jour tous les paquets installés
sudo apt upgrade -y
```

**Explication** : `apt upgrade` installe les mises à jour de sécurité et les mises à jour de paquets. Le flag `-y` répond automatiquement "oui" aux questions.

```bash
# Installer les mises à jour de sécurité automatiques
sudo apt install -y unattended-upgrades
sudo dpkg-reconfigure -plow unattended-upgrades
```

**Explication** : 
- `unattended-upgrades` installe automatiquement les mises à jour de sécurité
- `dpkg-reconfigure` configure le système pour installer automatiquement les mises à jour critiques

```bash
# Redémarrer si nécessaire
sudo reboot
```

**Explication** : Redémarre le serveur pour appliquer les mises à jour du noyau si nécessaire.

### 📌 ÉTAPE 2 : CRÉER UN UTILISATEUR NON-ROOT (OBLIGATOIRE)

**Pourquoi ?** Utiliser root expose le serveur à des risques de sécurité. Un utilisateur non-root avec sudo est plus sûr.

```bash
# Créer un nouvel utilisateur
sudo adduser heptanet
```

**Explication** : Crée un nouvel utilisateur système nommé `heptanet`. Vous serez invité à définir un mot de passe.

```bash
# Ajouter l'utilisateur au groupe sudo
sudo usermod -aG sudo heptanet
```

**Explication** : 
- `usermod` modifie un utilisateur existant
- `-aG` ajoute l'utilisateur au groupe spécifié (sans supprimer les autres groupes)
- `sudo` donne les privilèges administrateur

```bash
# Ajouter l'utilisateur au groupe docker (pour plus tard)
sudo usermod -aG docker heptanet
```

**Explication** : Ajoute l'utilisateur au groupe `docker` pour pouvoir exécuter Docker sans sudo.

```bash
# Se connecter avec le nouvel utilisateur
su - heptanet
```

**Explication** : Change d'utilisateur vers `heptanet`. Le `-` charge l'environnement complet de l'utilisateur.

### 📌 ÉTAPE 3 : CONFIGURER L'AUTHENTIFICATION PAR CLÉ SSH (OBLIGATOIRE)

**Pourquoi ?** Les clés SSH sont plus sécurisées que les mots de passe et résistent mieux aux attaques par force brute.

**Sur votre machine locale**, générez une clé SSH :

```bash
# Générer une clé SSH (sur votre machine locale)
ssh-keygen -t ed25519 -C "votre_email@example.com"
```

**Explication** :
- `ssh-keygen` génère une paire de clés SSH
- `-t ed25519` spécifie l'algorithme de chiffrement (plus sécurisé que RSA)
- `-C` ajoute un commentaire (votre email)

```bash
# Copier la clé publique vers le serveur
ssh-copy-id heptanet@51.83.72.29
```

**Explication** : `ssh-copy-id` copie automatiquement votre clé publique vers le serveur dans `~/.ssh/authorized_keys`.

**OU manuellement** :

```bash
# Afficher votre clé publique
cat ~/.ssh/id_ed25519.pub
```

**Explication** : Affiche le contenu de votre clé publique à copier.

**Sur le serveur** :

```bash
# Créer le répertoire .ssh si nécessaire
mkdir -p ~/.ssh
chmod 700 ~/.ssh
```

**Explication** :
- `mkdir -p` crée le répertoire et les parents si nécessaire
- `chmod 700` donne les permissions : propriétaire (lecture/écriture/exécution), autres (aucune)

```bash
# Ajouter votre clé publique
nano ~/.ssh/authorized_keys
# Collez votre clé publique ici (une ligne)
```

**Explication** : `nano` ouvre un éditeur de texte. Collez votre clé publique sur une nouvelle ligne.

```bash
# Sécuriser les permissions
chmod 600 ~/.ssh/authorized_keys
```

**Explication** : `chmod 600` donne les permissions : propriétaire (lecture/écriture), autres (aucune). SSH refuse les clés avec des permissions trop ouvertes.

### 📌 ÉTAPE 4 : DÉSACTIVER L'AUTHENTIFICATION PAR MOT DE PASSE (RECOMMANDÉ)

**Pourquoi ?** Une fois que vous pouvez vous connecter avec la clé SSH, désactiver les mots de passe empêche les attaques par force brute.

```bash
# Éditer la configuration SSH
sudo nano /etc/ssh/sshd_config
```

**Explication** : Ouvre le fichier de configuration SSH pour modification.

**Modifiez ces lignes** :

```conf
# Désactiver l'authentification par mot de passe
PasswordAuthentication no
PubkeyAuthentication yes

# Désactiver l'accès root direct
PermitRootLogin no

# Changer le port SSH (optionnel mais recommandé)
# Port 2222  # Décommentez et changez le port
```

**Explication** :
- `PasswordAuthentication no` : Désactive l'authentification par mot de passe
- `PubkeyAuthentication yes` : Active l'authentification par clé
- `PermitRootLogin no` : Empêche la connexion directe en root
- `Port 2222` : Change le port SSH (réduit les scans automatiques)

```bash
# Tester la configuration
sudo sshd -t
```

**Explication** : Vérifie la syntaxe du fichier de configuration sans redémarrer SSH.

```bash
# Si pas d'erreur, redémarrer SSH
sudo systemctl restart sshd
```

**Explication** : Redémarre le service SSH pour appliquer les changements.

⚠️ **IMPORTANT** : Testez une nouvelle connexion SSH dans un autre terminal avant de fermer la session actuelle !

### 📌 ÉTAPE 5 : INSTALLER ET CONFIGURER FAIL2BAN (PROTECTION CONTRE BRUTE FORCE)

**Pourquoi ?** Fail2Ban bloque automatiquement les IP qui tentent de se connecter avec de mauvais mots de passe.

```bash
# Installer Fail2Ban
sudo apt install -y fail2ban
```

**Explication** : Installe Fail2Ban depuis les dépôts Ubuntu.

```bash
# Créer la configuration locale
sudo cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local
```

**Explication** : Copie le fichier de configuration par défaut. Les modifications dans `.local` ne seront pas écrasées par les mises à jour.

```bash
# Éditer la configuration
sudo nano /etc/fail2ban/jail.local
```

**Modifiez ces valeurs** :

```ini
[DEFAULT]
bantime = 3600          # Bannir pendant 1 heure
findtime = 600          # Fenêtre de 10 minutes
maxretry = 5            # 5 tentatives avant bannissement

[sshd]
enabled = true
port = ssh              # Ou votre port personnalisé
logpath = %(sshd_log)s
backend = %(sshd_backend)s
```

**Explication** :
- `bantime` : Durée du bannissement en secondes
- `findtime` : Fenêtre de temps pour compter les tentatives
- `maxretry` : Nombre de tentatives avant bannissement

```bash
# Démarrer et activer Fail2Ban
sudo systemctl start fail2ban
sudo systemctl enable fail2ban
```

**Explication** :
- `start` : Démarre le service immédiatement
- `enable` : Active le service au démarrage du système

```bash
# Vérifier le statut
sudo systemctl status fail2ban
```

**Explication** : Affiche l'état du service Fail2Ban.

```bash
# Voir les IP bannies
sudo fail2ban-client status sshd
```

**Explication** : Affiche les IP actuellement bannies par Fail2Ban pour SSH.

### 📌 ÉTAPE 6 : CONFIGURER LE FIREWALL (UFW) - BASE

**Pourquoi ?** Un firewall bloque les connexions non autorisées et protège votre serveur.

```bash
# Installer UFW
sudo apt install -y ufw
```

**Explication** : Installe Uncomplicated Firewall (UFW), un firewall simple pour Ubuntu.

```bash
# Vérifier le statut
sudo ufw status
```

**Explication** : Affiche l'état actuel du firewall.

```bash
# Autoriser SSH (IMPORTANT : faites-le en premier !)
sudo ufw allow OpenSSH
```

**Explication** : Autorise les connexions SSH. **CRITIQUE** : Faites-le avant d'activer le firewall pour ne pas être bloqué !

```bash
# OU si vous avez changé le port SSH :
sudo ufw allow 2222/tcp comment 'SSH Custom Port'
```

**Explication** : Autorise le port 2222 avec un commentaire pour documentation.

```bash
# Autoriser HTTP (pour NGINX)
sudo ufw allow 80/tcp comment 'HTTP - NGINX'
```

**Explication** : Autorise le port 80 (HTTP) pour que NGINX puisse recevoir les requêtes.

```bash
# Bloquer tout le reste par défaut
sudo ufw default deny incoming
sudo ufw default allow outgoing
```

**Explication** :
- `default deny incoming` : Bloque toutes les connexions entrantes par défaut
- `default allow outgoing` : Autorise toutes les connexions sortantes

```bash
# Activer le firewall
sudo ufw enable
```

**Explication** : Active le firewall. Vous serez invité à confirmer.

```bash
# Vérifier le statut
sudo ufw status verbose
```

**Explication** : Affiche l'état détaillé du firewall avec toutes les règles.

### 📌 ÉTAPE 7 : DÉSACTIVER LES SERVICES INUTILES

**Pourquoi ?** Moins de services = moins de surface d'attaque.

```bash
# Voir les services actifs
sudo systemctl list-units --type=service --state=running
```

**Explication** : Liste tous les services système actuellement en cours d'exécution.

```bash
# Désactiver les services inutiles (exemples)
sudo systemctl disable bluetooth
sudo systemctl disable avahi-daemon
```

**Explication** : `disable` empêche le service de démarrer automatiquement au boot.

### 📌 ÉTAPE 8 : CONFIGURER LES LOGS ET LA ROTATION

**Pourquoi ?** Les logs peuvent remplir le disque. La rotation les archive automatiquement.

```bash
# Installer logrotate si nécessaire
sudo apt install -y logrotate
```

**Explication** : Installe logrotate pour gérer la rotation automatique des logs.

```bash
# Vérifier la configuration
sudo logrotate -d /etc/logrotate.conf
```

**Explication** : `-d` teste la configuration en mode debug sans appliquer les changements.

### 📌 ÉTAPE 9 : CONFIGURER LES LIMITES DE RESSOURCES

**Pourquoi ?** Limite l'utilisation des ressources pour éviter les dénis de service.

```bash
# Éditer les limites système
sudo nano /etc/security/limits.conf
```

**Ajoutez ces lignes** :

```conf
# Limites pour l'utilisateur heptanet
heptanet soft nofile 65535
heptanet hard nofile 65535
heptanet soft nproc 4096
heptanet hard nproc 4096
```

**Explication** :
- `nofile` : Nombre maximum de fichiers ouverts simultanément
- `nproc` : Nombre maximum de processus
- `soft` : Limite souple (peut être dépassée temporairement)
- `hard` : Limite dure (ne peut pas être dépassée)

---

## 5. CONNEXION SSH AU VPS

### 📌 ÉTAPE 1 : CONNEXION INITIALE

```bash
# Connexion avec l'utilisateur créé
ssh heptanet@51.83.72.29
```

**Explication** : Se connecte au serveur avec l'utilisateur `heptanet`. La première fois, vous devrez accepter la clé du serveur.

```bash
# OU si vous avez changé le port SSH
ssh -p 2222 heptanet@51.83.72.29
```

**Explication** : `-p` spécifie le port SSH personnalisé.

### 📌 ÉTAPE 2 : VÉRIFICATION DE L'ENVIRONNEMENT

```bash
# Vérifier la version d'Ubuntu
lsb_release -a
```

**Explication** : Affiche les informations de distribution Linux (version, nom, etc.).

```bash
# Vérifier l'espace disque
df -h
```

**Explication** : `df -h` affiche l'espace disque utilisé et disponible de manière lisible (`-h` = human readable).

```bash
# Vérifier la RAM
free -h
```

**Explication** : Affiche l'utilisation de la mémoire RAM de manière lisible.

```bash
# Vérifier les processus
top
```

**Explication** : Affiche les processus en cours d'exécution. Appuyez sur `q` pour quitter.

---

## 6. INSTALLATION DE DOCKER ET DOCKER COMPOSE

### 📌 ÉTAPE 1 : INSTALLER DOCKER

```bash
# Installer les dépendances
sudo apt install -y apt-transport-https ca-certificates curl gnupg lsb-release
```

**Explication** : Installe les outils nécessaires pour ajouter le dépôt Docker officiel.

```bash
# Ajouter la clé GPG officielle de Docker
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
```

**Explication** :
- `curl` télécharge la clé GPG de Docker
- `gpg --dearmor` convertit la clé au format binaire
- La clé est stockée dans `/usr/share/keyrings/` pour la vérification des paquets

```bash
# Ajouter le repository Docker
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
```

**Explication** :
- `dpkg --print-architecture` : Détecte l'architecture (amd64, arm64, etc.)
- `lsb_release -cs` : Récupère le nom de code de la distribution (jammy pour Ubuntu 22.04)
- Ajoute le dépôt Docker officiel à la liste des sources

```bash
# Mettre à jour les paquets
sudo apt update
```

**Explication** : Met à jour la liste des paquets avec le nouveau dépôt Docker.

```bash
# Installer Docker Engine
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

**Explication** :
- `docker-ce` : Docker Community Edition (moteur principal)
- `docker-ce-cli` : Interface en ligne de commande
- `containerd.io` : Runtime de conteneurs
- `docker-buildx-plugin` : Outil de build avancé
- `docker-compose-plugin` : Docker Compose intégré

```bash
# Démarrer Docker
sudo systemctl start docker
sudo systemctl enable docker
```

**Explication** :
- `start` : Démarre Docker immédiatement
- `enable` : Active Docker au démarrage du système

### 📌 ÉTAPE 2 : AJOUTER L'UTILISATEUR AU GROUPE DOCKER

```bash
# Ajouter l'utilisateur au groupe docker
sudo usermod -aG docker $USER
```

**Explication** : Ajoute l'utilisateur actuel (`$USER`) au groupe `docker` pour exécuter Docker sans sudo.

```bash
# Appliquer les changements (se déconnecter/reconnecter)
newgrp docker
```

**Explication** : Active le nouveau groupe sans se déconnecter. Ou déconnectez-vous et reconnectez-vous.

```bash
# Vérifier que Docker fonctionne
docker --version
docker ps
```

**Explication** :
- `docker --version` : Affiche la version de Docker
- `docker ps` : Liste les conteneurs en cours d'exécution (vide au début)

### 📌 ÉTAPE 3 : SÉCURISER DOCKER

```bash
# Éditer la configuration Docker
sudo nano /etc/docker/daemon.json
```

**Ajoutez cette configuration** :

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "userland-proxy": false,
  "no-new-privileges": true
}
```

**Explication** :
- `log-driver` : Format des logs (json-file)
- `max-size` : Taille maximale d'un fichier de log (10 Mo)
- `max-file` : Nombre maximum de fichiers de log (3 = rotation automatique)
- `userland-proxy` : Désactive le proxy utilisateur (plus sécurisé)
- `no-new-privileges` : Empêche les conteneurs d'obtenir de nouveaux privilèges

```bash
# Redémarrer Docker
sudo systemctl restart docker
```

**Explication** : Redémarre Docker pour appliquer la nouvelle configuration.

### 📌 ÉTAPE 4 : VÉRIFIER L'INSTALLATION

```bash
# Vérifier que tout fonctionne
docker --version
docker compose version
docker ps
```

**Explication** : Vérifie que Docker et Docker Compose sont installés et fonctionnels.

⏱️ **Temps estimé** : 10-15 minutes

---

## 7. CONFIGURATION DE L'ENVIRONNEMENT

### 📌 ÉTAPE 1 : CRÉER LA STRUCTURE DE RÉPERTOIRES

```bash
# Créer le répertoire principal
sudo mkdir -p /srv/heptanet
```

**Explication** : `mkdir -p` crée le répertoire `/srv/heptanet` et tous les parents nécessaires. `/srv` est l'emplacement standard pour les données de service.

```bash
# Créer les sous-répertoires
sudo mkdir -p /srv/heptanet/backend
sudo mkdir -p /srv/heptanet/nginx
sudo mkdir -p /srv/heptanet/postgres
sudo mkdir -p /srv/heptanet/data/db
sudo mkdir -p /srv/heptanet/data/uploads
sudo mkdir -p /srv/heptanet/backups
sudo mkdir -p /srv/heptanet/logs
```

**Explication** : Crée la structure de répertoires pour organiser les fichiers :
- `backend` : Code source de l'application
- `nginx` : Configuration NGINX
- `postgres` : Configuration PostgreSQL
- `data/db` : Données de la base de données
- `data/uploads` : Fichiers uploadés par l'API
- `backups` : Sauvegardes
- `logs` : Logs applicatifs

```bash
# Donner les permissions à l'utilisateur
sudo chown -R $USER:$USER /srv/heptanet
```

**Explication** : `chown -R` change récursivement le propriétaire de tous les fichiers et répertoires à l'utilisateur actuel.

```bash
# Sécuriser les permissions
chmod 750 /srv/heptanet
chmod 700 /srv/heptanet/backups
```

**Explication** :
- `750` : Propriétaire (lecture/écriture/exécution), groupe (lecture/exécution), autres (aucune)
- `700` : Propriétaire uniquement (lecture/écriture/exécution)

```bash
# Vérifier la structure
tree /srv/heptanet -L 2
```

**Explication** : Affiche l'arborescence des répertoires jusqu'à 2 niveaux de profondeur.

Si `tree` n'est pas installé :
```bash
sudo apt install -y tree
```

### 📌 ÉTAPE 2 : INSTALLER GIT

```bash
# Installer Git
sudo apt install -y git
```

**Explication** : Installe Git, le système de contrôle de version.

```bash
# Configurer Git (remplacez par vos informations)
git config --global user.name "HeptaNet Server"
git config --global user.email "server@heptanet.com"
```

**Explication** : Configure Git avec un nom et un email pour les commits (utilisé par le serveur).

---

## 8. STRUCTURE DU PROJET SUR LE SERVEUR

### 📌 STRUCTURE FINALE ATTENDUE

```
/srv/heptanet/
├── backend/
│   ├── Dockerfile
│   ├── appsettings.Production.json
│   └── [code source cloné depuis GitHub]
├── nginx/
│   └── nginx.conf
├── postgres/
│   ├── postgres.conf
│   └── init.sql (optionnel)
├── data/
│   ├── db/ (données PostgreSQL)
│   └── uploads/ (fichiers uploadés par l'API)
├── backups/ (sauvegardes de la base de données)
├── logs/ (logs applicatifs)
└── docker-compose.yml
```

### 📌 ÉTAPE 1 : CLONER LE REPOSITORY GITHUB

```bash
# Aller dans le répertoire backend
cd /srv/heptanet/backend
```

**Explication** : Change de répertoire vers le dossier backend.

```bash
# Cloner le repository (remplacez par votre URL)
# Option 1 : Repository public
git clone https://github.com/VOTRE_USERNAME/heptanet-backend.git .
```

**Explication** :
- `git clone` : Clone le repository GitHub
- Le `.` à la fin clone dans le répertoire actuel au lieu de créer un sous-dossier

```bash
# Option 2 : Repository privé (nécessite un token GitHub)
# git clone https://VOTRE_TOKEN@github.com/VOTRE_USERNAME/heptanet-backend.git .
```

**Explication** : Pour un repository privé, incluez un Personal Access Token dans l'URL.

```bash
# Option 3 : Avec SSH (si vous avez configuré une clé SSH)
# git clone git@github.com:VOTRE_USERNAME/heptanet-backend.git .
```

**Explication** : Utilise une clé SSH pour l'authentification (plus sécurisé pour les repos privés).

💡 **Si le repository est privé**, créez un Personal Access Token sur GitHub (Settings → Developer settings → Personal access tokens).

### 📌 ÉTAPE 2 : VÉRIFIER LE CODE CLONÉ

```bash
# Vérifier que les fichiers sont présents
ls -la
```

**Explication** : `ls -la` liste tous les fichiers (y compris cachés) avec les détails.

Vous devriez voir :
- `HeptaNet.API/`
- `HeptaNet.Application/`
- `HeptaNet.Domain/`
- `HeptaNet.Infrastructure/`
- `HeptaNet.sln`

---

## 9. CONFIGURATION DU BACKEND .NET 9

### 📌 ÉTAPE 1 : CRÉER LE DOCKERFILE

```bash
# Créer le Dockerfile dans /srv/heptanet/backend
cd /srv/heptanet/backend
nano Dockerfile
```

**Explication** : Ouvre l'éditeur nano pour créer le fichier Dockerfile.

**Collez ce contenu** :

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copier les fichiers .csproj et restaurer les dépendances
COPY ["HeptaNet.API/HeptaNet.API.csproj", "HeptaNet.API/"]
COPY ["HeptaNet.Application/HeptaNet.Application.csproj", "HeptaNet.Application/"]
COPY ["HeptaNet.Domain/HeptaNet.Domain.csproj", "HeptaNet.Domain/"]
COPY ["HeptaNet.Infrastructure/HeptaNet.Infrastructure.csproj", "HeptaNet.Infrastructure/"]

RUN dotnet restore "HeptaNet.API/HeptaNet.API.csproj"

# Copier tout le code source
COPY . .

# Builder et publier l'application
WORKDIR "/src/HeptaNet.API"
RUN dotnet build "HeptaNet.API.csproj" -c Release -o /app/build
RUN dotnet publish "HeptaNet.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Créer un utilisateur non-root pour la sécurité
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Créer un répertoire pour les uploads
RUN mkdir -p /app/uploads/voice-messages && \
    chown -R appuser:appuser /app/uploads

# Copier les fichiers publiés
COPY --from=build /app/publish .

# Changer le propriétaire
RUN chown -R appuser:appuser /app

# Utiliser l'utilisateur non-root
USER appuser

# Exposer le port 5106
EXPOSE 5106

# Point d'entrée
ENTRYPOINT ["dotnet", "HeptaNet.API.dll"]
```

**Explication du Dockerfile** :
- **Stage 1 (build)** : Utilise l'image SDK pour compiler l'application
- **Stage 2 (runtime)** : Utilise l'image runtime plus légère
- **Utilisateur non-root** : Améliore la sécurité en évitant d'exécuter en root
- **Multi-stage build** : Réduit la taille de l'image finale

Sauvegarder avec `Ctrl+O`, puis `Enter`, puis `Ctrl+X` pour quitter.

### 📌 ÉTAPE 2 : CRÉER LE FICHIER appsettings.Production.json

```bash
cd /srv/heptanet/backend/HeptaNet.API
nano appsettings.Production.json
```

**Collez ce contenu** (remplacez les valeurs par les vôtres) :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=heptanet_db;Username=heptanet_user;Password=VOTRE_MOT_DE_PASSE_POSTGRES_FORT;"
  },
  "JwtSettings": {
    "SecretKey": "VOTRE_CLE_SECRETE_JWT_TRES_LONGUE_ET_SECURISEE_MINIMUM_64_CARACTERES_POUR_PRODUCTION_2025",
    "Issuer": "HeptaNet",
    "Audience": "HeptaNetUsers",
    "ExpiryInMinutes": 60
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "User": "votre.email@gmail.com",
    "Password": "votre_mot_de_passe_application",
    "From": "votre.email@gmail.com",
    "FromName": "HeptaNet"
  },
  "Otp": {
    "Length": 6,
    "ExpiryMinutes": 10,
    "ResendCooldownSeconds": 60,
    "MaxSendsPerDay": 5,
    "MaxAttempts": 5
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ASPNETCORE_ENVIRONMENT": "Production",
  "ASPNETCORE_URLS": "http://0.0.0.0:5106"
}
```

💡 **IMPORTANT** : 
- Remplacez `VOTRE_MOT_DE_PASSE_POSTGRES_FORT` par un mot de passe fort (générez avec `openssl rand -base64 32`)
- Remplacez `VOTRE_CLE_SECRETE_JWT_...` par une clé secrète très longue (minimum 64 caractères)
- Ne commitez JAMAIS ce fichier dans Git (ajoutez-le au .gitignore)

Sauvegarder avec `Ctrl+O`, puis `Enter`, puis `Ctrl+X`.

### 📌 ÉTAPE 3 : MODIFIER Program.cs POUR CORS (AUTORISER L'IP)

Modifiez le fichier `HeptaNet.API/Program.cs` pour autoriser votre IP :

```csharp
// Dans la section CORS (ligne ~65)
// Pour la production, listez vos origines spécifiques
string[] allowedOrigins = {
    "http://51.83.72.29",              // Votre IP VPS
    "http://localhost:3000",          // Frontend local React
    "http://localhost:5173",          // Frontend local Vite
    "http://localhost:8080",          // Frontend local Vue
    "http://localhost:4200",          // Frontend local Angular
    // Ajoutez d'autres origines si nécessaire
};
return allowedOrigins.Contains(origin);
```

**Explication** : Autorise uniquement les origines spécifiées pour les requêtes CORS, améliorant la sécurité.

### 📌 ÉTAPE 4 : CRÉER UN FICHIER .dockerignore

```bash
cd /srv/heptanet/backend
nano .dockerignore
```

**Collez ce contenu** :

```
**/.git
**/.vs
**/.vscode
**/bin
**/obj
**/node_modules
**/.dockerignore
**/Dockerfile
**/docker-compose*.yml
**/*.md
**/.env
**/uploads
**/appsettings.Development.json
```

**Explication** : Exclut les fichiers inutiles du contexte Docker, réduisant la taille et accélérant le build.

Sauvegarder avec `Ctrl+O`, puis `Enter`, puis `Ctrl+X`.

### 📌 ÉTAPE 5 : SÉCURISER LE FICHIER appsettings.Production.json

```bash
# Restreindre les permissions
chmod 600 /srv/heptanet/backend/HeptaNet.API/appsettings.Production.json
```

**Explication** : `chmod 600` donne les permissions : propriétaire (lecture/écriture), autres (aucune). Protège les secrets.

```bash
# Vérifier
ls -la /srv/heptanet/backend/HeptaNet.API/appsettings.Production.json
```

**Explication** : Vérifie que les permissions sont correctes (devrait afficher `-rw-------`).

---

## 10. CONFIGURATION POSTGRESQL (SÉCURISÉE)

### 📌 ÉTAPE 1 : CRÉER LE FICHIER DE CONFIGURATION POSTGRESQL

```bash
cd /srv/heptanet/postgres
nano postgres.conf
```

**Collez ce contenu** (ajustez selon vos ressources) :

```conf
# Configuration PostgreSQL optimisée pour production
# Ajustez selon la RAM disponible sur votre serveur

# Mémoire partagée (recommandé: 25% de la RAM totale)
shared_buffers = 2GB

# Cache effectif (recommandé: 50-75% de la RAM totale)
effective_cache_size = 6GB

# Mémoire de travail par opération
work_mem = 20MB

# Mémoire pour maintenance
maintenance_work_mem = 512MB

# Configuration WAL (Write-Ahead Logging)
checkpoint_completion_target = 0.9
max_wal_size = 2GB
min_wal_size = 512MB

# Performance
random_page_cost = 1.1
effective_io_concurrency = 200

# Sécurité
password_encryption = scram-sha-256
ssl = off  # Pas de SSL dans Docker (réseau interne)

# Logging
log_destination = 'stderr'
logging_collector = on
log_directory = 'log'
log_filename = 'postgresql-%Y-%m-%d_%H%M%S.log'
log_statement = 'ddl'  # Logger les DDL (CREATE, ALTER, DROP)
log_duration = on
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '
```

💡 **Ajustez `shared_buffers` et `effective_cache_size` selon votre RAM** :
- 4 GB RAM : shared_buffers=1GB, effective_cache_size=3GB
- 8 GB RAM : shared_buffers=2GB, effective_cache_size=6GB
- 16 GB RAM : shared_buffers=4GB, effective_cache_size=12GB

Sauvegarder avec `Ctrl+O`, puis `Enter`, puis `Ctrl+X`.

### 📌 ÉTAPE 2 : GÉNÉRER UN MOT DE PASSE FORT POUR POSTGRESQL

```bash
# Générer un mot de passe fort
openssl rand -base64 32
```

**Explication** : `openssl rand -base64 32` génère 32 octets aléatoires encodés en base64 (environ 44 caractères).

```bash
# OU utiliser pwgen si installé
sudo apt install -y pwgen
pwgen -s 32 1
```

**Explication** : `pwgen -s 32 1` génère un mot de passe sécurisé de 32 caractères.

💡 **Copiez ce mot de passe** et utilisez-le dans `docker-compose.yml` et `appsettings.Production.json`.

---

## 11. CONFIGURATION NGINX (REVERSE PROXY - IP UNIQUEMENT)

### 📌 ÉTAPE 1 : CRÉER LA CONFIGURATION NGINX

```bash
cd /srv/heptanet/nginx
nano nginx.conf
```

**Collez ce contenu** :

```nginx
events {
    worker_connections 1024;
}

http {
    include       /etc/nginx/mime.types;
    default_type  application/octet-stream;

    # Logging
    access_log /var/log/nginx/access.log;
    error_log /var/log/nginx/error.log;

    # Performance
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;
    client_max_body_size 50M;  # Pour les uploads de fichiers

    # Sécurité - Headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "no-referrer-when-downgrade" always;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml text/javascript application/json application/javascript application/xml+rss;

    # Rate limiting (protection contre DDoS)
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;
    limit_req_zone $binary_remote_addr zone=auth_limit:10m rate=5r/s;

    # Upstream pour le backend
    upstream backend {
        server backend:5106;
        keepalive 32;
    }

    # Configuration HTTP (pas de SSL car IP uniquement)
    server {
        listen 80;
        server_name 51.83.72.29;  # Votre IP VPS

        # Rate limiting pour l'API
        location /api/ {
            limit_req zone=api_limit burst=20 nodelay;
            limit_req_status 429;

            proxy_pass http://backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;

            # Timeouts pour SignalR
            proxy_connect_timeout 60s;
            proxy_send_timeout 60s;
            proxy_read_timeout 60s;
        }

        # Rate limiting pour l'authentification
        location /api/auth/ {
            limit_req zone=auth_limit burst=10 nodelay;
            limit_req_status 429;

            proxy_pass http://backend;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # Routes SignalR
        location /hubs/ {
            proxy_pass http://backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;

            # Timeouts étendus pour WebSocket
            proxy_connect_timeout 7d;
            proxy_send_timeout 7d;
            proxy_read_timeout 7d;
        }

        # Toutes les autres routes
        location / {
            proxy_pass http://backend;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;
        }

        # Bloquer l'accès aux fichiers sensibles
        location ~ /\. {
            deny all;
            access_log off;
            log_not_found off;
        }
    }
}
```

**Explication de la configuration NGINX** :
- **Rate limiting** : Limite le nombre de requêtes par IP (protection DDoS)
- **Headers de sécurité** : Protège contre XSS, clickjacking, etc.
- **Gzip** : Compresse les réponses pour améliorer les performances
- **Upstream** : Définit le serveur backend
- **Proxy headers** : Transmet les informations du client au backend

Sauvegarder avec `Ctrl+O`, puis `Enter`, puis `Ctrl+X`.

### 📌 ÉTAPE 2 : TESTER LA CONFIGURATION NGINX

```bash
# Tester la syntaxe (avant de démarrer)
docker run --rm -v /srv/heptanet/nginx/nginx.conf:/etc/nginx/nginx.conf:ro nginx nginx -t
```

**Explication** : Teste la syntaxe du fichier de configuration NGINX dans un conteneur temporaire.

---

## 12. CONFIGURATION DOCKER COMPOSE

### 📌 ÉTAPE 1 : CRÉER LE FICHIER docker-compose.yml

```bash
cd /srv/heptanet
nano docker-compose.yml
```

**Collez ce contenu** :

```yaml
version: '3.9'

services:
  # Service PostgreSQL
  postgres:
    image: postgres:16
    container_name: heptanet_postgres
    restart: always
    environment:
      POSTGRES_DB: heptanet_db
      POSTGRES_USER: heptanet_user
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}  # Utiliser une variable d'environnement
      PGDATA: /var/lib/postgresql/data/pgdata
    ports:
      - "127.0.0.1:5432:5432"  # Écouter uniquement sur localhost (sécurité)
    volumes:
      - ./data/db:/var/lib/postgresql/data
      - ./postgres/postgres.conf:/etc/postgresql/postgresql.conf
    command: postgres -c config_file=/etc/postgresql/postgresql.conf
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U heptanet_user -d heptanet_db"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - heptanet_network
    security_opt:
      - no-new-privileges:true
    read_only: false  # PostgreSQL a besoin d'écriture
    tmpfs:
      - /tmp
      - /var/run/postgresql

  # Service Backend .NET
  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: heptanet_backend
    restart: always
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://0.0.0.0:5106
    ports:
      - "127.0.0.1:5106:5106"  # Écouter uniquement sur localhost (sécurité)
    volumes:
      - ./data/uploads:/app/uploads
      - ./backend/HeptaNet.API/appsettings.Production.json:/app/appsettings.Production.json:ro
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - heptanet_network
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:5106/health || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    security_opt:
      - no-new-privileges:true
    read_only: true
    tmpfs:
      - /tmp
      - /app/uploads  # Mount temporaire pour les uploads

  # Service NGINX
  nginx:
    image: nginx:latest
    container_name: heptanet_nginx
    restart: always
    ports:
      - "80:80"  # Port public pour HTTP
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
    depends_on:
      - backend
    networks:
      - heptanet_network
    security_opt:
      - no-new-privileges:true
    read_only: true
    tmpfs:
      - /tmp
      - /var/cache/nginx
      - /var/run

networks:
  heptanet_network:
    driver: bridge
    internal: false  # Permet l'accès externe via NGINX

volumes:
  postgres_data:
    driver: local
```

**Explication de la configuration Docker Compose** :
- **127.0.0.1:5432** : PostgreSQL n'écoute que sur localhost (pas accessible depuis l'extérieur)
- **127.0.0.1:5106** : Backend n'écoute que sur localhost (protégé par NGINX)
- **no-new-privileges** : Empêche les conteneurs d'obtenir de nouveaux privilèges
- **read_only** : Monte le système de fichiers en lecture seule (sécurité)
- **tmpfs** : Utilise la RAM pour les fichiers temporaires
- **healthcheck** : Vérifie automatiquement la santé des services

Sauvegarder avec `Ctrl+O`, puis `Enter`, puis `Ctrl+X`.

### 📌 ÉTAPE 2 : CRÉER UN FICHIER .env POUR LES SECRETS

```bash
cd /srv/heptanet
nano .env
```

**Collez ce contenu** :

```env
# Mot de passe PostgreSQL (générez-en un fort avec: openssl rand -base64 32)
POSTGRES_PASSWORD=VOTRE_MOT_DE_PASSE_POSTGRES_FORT_ICI
```

💡 **IMPORTANT** : 
- Générez un mot de passe fort avec `openssl rand -base64 32`
- Ne commitez JAMAIS ce fichier dans Git
- Utilisez le même mot de passe dans `appsettings.Production.json`

Sauvegarder avec `Ctrl+O`, puis `Enter`, puis `Ctrl+X`.

### 📌 ÉTAPE 3 : SÉCURISER LE FICHIER .env

```bash
# Restreindre les permissions
chmod 600 /srv/heptanet/.env
```

**Explication** : Protège le fichier contenant les secrets.

```bash
# Vérifier
ls -la /srv/heptanet/.env
```

**Explication** : Vérifie les permissions (devrait afficher `-rw-------`).

### 📌 ÉTAPE 4 : MODIFIER docker-compose.yml POUR UTILISER .env

Le fichier `docker-compose.yml` utilise déjà `${POSTGRES_PASSWORD}`, donc Docker Compose chargera automatiquement le fichier `.env`.

---

## 13. CONFIGURATION DU FIREWALL (UFW) - COMPLÈTE

### 📌 ÉTAPE 1 : CONFIGURER UFW COMPLÈTEMENT

```bash
# Vérifier le statut actuel
sudo ufw status verbose
```

**Explication** : Affiche l'état actuel du firewall avec toutes les règles.

```bash
# Autoriser SSH (IMPORTANT : faites-le en premier !)
sudo ufw allow OpenSSH
```

**Explication** : Autorise les connexions SSH. **CRITIQUE** : Faites-le avant d'activer le firewall !

```bash
# OU si vous avez changé le port SSH :
sudo ufw allow 2222/tcp comment 'SSH Custom Port'
```

**Explication** : Autorise le port SSH personnalisé avec un commentaire.

```bash
# Autoriser HTTP (pour NGINX)
sudo ufw allow 80/tcp comment 'HTTP - NGINX'
```

**Explication** : Autorise le port 80 pour que NGINX puisse recevoir les requêtes HTTP.

```bash
# Bloquer tout le reste par défaut
sudo ufw default deny incoming
sudo ufw default allow outgoing
```

**Explication** :
- `default deny incoming` : Bloque toutes les connexions entrantes par défaut
- `default allow outgoing` : Autorise toutes les connexions sortantes

```bash
# Activer le firewall
sudo ufw enable
```

**Explication** : Active le firewall. Vous serez invité à confirmer.

```bash
# Vérifier le statut
sudo ufw status numbered
```

**Explication** : Affiche toutes les règles avec des numéros pour faciliter la gestion.

### 📌 ÉTAPE 2 : CONFIGURER LES RÈGLES DE RATE LIMITING (OPTIONNEL)

```bash
# Installer ufw-extras pour le rate limiting
sudo apt install -y ufw-extras
```

**Explication** : Installe des extensions pour UFW permettant le rate limiting.

```bash
# Limiter les connexions SSH
sudo ufw limit ssh/tcp comment 'Rate limit SSH'
```

**Explication** : Limite le nombre de tentatives de connexion SSH par IP (protection contre brute force).

### 📌 ÉTAPE 3 : VÉRIFIER QUE LES PORTS SONT OUVERTS

```bash
# Vérifier les ports ouverts
sudo netstat -tulpn | grep LISTEN
```

**Explication** : Affiche tous les ports en écoute. Vous devriez voir :
- Port 22 (SSH) ou votre port personnalisé
- Port 80 (HTTP - NGINX)
- Port 5432 (PostgreSQL - localhost uniquement)
- Port 5106 (Backend - localhost uniquement)

---

## ✅ CHECKLIST DE SÉCURITÉ - PARTIE 1

- [ ] Utilisateur non-root créé et configuré
- [ ] Authentification SSH par clé configurée
- [ ] Authentification par mot de passe SSH désactivée
- [ ] Fail2Ban installé et configuré
- [ ] Firewall UFW activé et configuré
- [ ] Docker installé et sécurisé
- [ ] Structure de répertoires créée avec permissions correctes
- [ ] Repository GitHub cloné
- [ ] Dockerfile créé avec utilisateur non-root
- [ ] appsettings.Production.json créé avec permissions 600
- [ ] Configuration PostgreSQL sécurisée
- [ ] Configuration NGINX avec rate limiting et headers de sécurité
- [ ] docker-compose.yml avec sécurité (no-new-privileges, read_only)
- [ ] Fichier .env créé avec permissions 600

---

**📌 Suite dans le fichier suivant : `02-Deploiement-et-CICD.md`**


