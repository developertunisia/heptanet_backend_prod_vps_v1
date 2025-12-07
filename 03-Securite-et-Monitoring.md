# 📘 DOCUMENTATION TECHNIQUE - PARTIE 3 : Sécurité Avancée et Troubleshooting
## Architecture Docker (Backend .NET 9 + PostgreSQL + NGINX) - Ubuntu 22.04
### VPS IP: 51.83.72.29 | Configuration Sécurisée

---

## 📋 TABLE DES MATIÈRES

1. [Sécurité avancée](#1-sécurité-avancée)
2. [Monitoring et alertes](#2-monitoring-et-alertes)
3. [Troubleshooting](#3-troubleshooting)
4. [Commandes de référence](#4-commandes-de-référence)
5. [Checklist de sécurité complète](#5-checklist-de-sécurité-complète)

---

## 1. SÉCURITÉ AVANCÉE

### 📌 ÉTAPE 1 : CONFIGURER LES LIMITES DE RESSOURCES POUR DOCKER

**Pourquoi ?** Limite l'utilisation des ressources pour éviter les dénis de service et les attaques par épuisement des ressources.

```bash
# Éditer la configuration Docker
sudo nano /etc/docker/daemon.json
```

**Explication** : Ouvre le fichier de configuration Docker pour modification.

**Ajoutez cette configuration** :

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "userland-proxy": false,
  "no-new-privileges": true,
  "default-ulimits": {
    "nofile": {
      "Name": "nofile",
      "Hard": 64000,
      "Soft": 64000
    }
  }
}
```

**Explication** :
- **`log-driver`** : Format des logs (json-file)
- **`max-size`** : Taille maximale d'un fichier de log (10 Mo)
- **`max-file`** : Nombre maximum de fichiers de log (3 = rotation automatique)
- **`userland-proxy`** : Désactive le proxy utilisateur (plus sécurisé)
- **`no-new-privileges`** : Empêche les conteneurs d'obtenir de nouveaux privilèges
- **`default-ulimits`** : Limite le nombre de fichiers ouverts simultanément

```bash
# Redémarrer Docker
sudo systemctl restart docker
```

**Explication** : Redémarre Docker pour appliquer la nouvelle configuration.

```bash
# Vérifier que Docker fonctionne toujours
docker ps
```

**Explication** : Vérifie que Docker fonctionne correctement après le redémarrage.

### 📌 ÉTAPE 2 : CONFIGURER LES SCANS DE SÉCURITÉ AUTOMATIQUES

**Pourquoi ?** Détecte les malwares et les fichiers suspects sur le serveur.

```bash
# Installer ClamAV (antivirus)
sudo apt install -y clamav clamav-daemon
```

**Explication** :
- **ClamAV** : Antivirus open-source pour Linux
- **clamav-daemon** : Service en arrière-plan pour les scans automatiques

```bash
# Mettre à jour les définitions de virus
sudo freshclam
```

**Explication** : Télécharge les dernières définitions de virus depuis les serveurs ClamAV.

```bash
# Scanner le système (optionnel, peut être long)
# sudo clamscan -r /srv/heptanet
```

**Explication** :
- **`clamscan`** : Lance un scan antivirus
- **`-r`** : Récursif (scanne tous les sous-répertoires)
- **Note** : Cette commande peut prendre beaucoup de temps sur un grand système

**Créer un script de scan automatique** :

```bash
# Créer un script de scan quotidien
sudo nano /usr/local/bin/clamscan-daily.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

LOG_FILE="/var/log/clamav-scan.log"
DATE=$(date '+%Y-%m-%d %H:%M:%S')

echo "[$DATE] Début du scan ClamAV" >> $LOG_FILE

# Scanner uniquement les répertoires critiques
clamscan -r -i /srv/heptanet/data/uploads >> $LOG_FILE 2>&1
clamscan -r -i /srv/heptanet/backups >> $LOG_FILE 2>&1

echo "[$DATE] Scan terminé" >> $LOG_FILE
```

**Explication** : Scanne uniquement les répertoires critiques (uploads et backups) pour ne pas surcharger le système.

```bash
# Rendre exécutable
sudo chmod +x /usr/local/bin/clamscan-daily.sh
```

**Ajouter au crontab** :

```bash
crontab -e
```

**Ajoutez** :

```cron
# Scan antivirus quotidien à 3h du matin
0 3 * * * /usr/local/bin/clamscan-daily.sh
```

### 📌 ÉTAPE 3 : CONFIGURER LES ALERTES DE SÉCURITÉ

**Pourquoi ?** Détecte les modifications non autorisées des fichiers système.

```bash
# Installer et configurer AIDE pour la détection d'intrusion
sudo apt install -y aide
```

**Explication** : **AIDE** (Advanced Intrusion Detection Environment) surveille l'intégrité des fichiers système.

```bash
# Initialiser la base de données
sudo aideinit
```

**Explication** : Crée une base de données avec les checksums de tous les fichiers système importants.

```bash
# Créer un script de vérification quotidienne
sudo nano /usr/local/bin/aide-check.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

LOG_FILE="/var/log/aide/aide-check.log"
DATE=$(date '+%Y-%m-%d %H:%M:%S')

echo "[$DATE] Début de la vérification AIDE" >> $LOG_FILE

/usr/bin/aide --check >> $LOG_FILE 2>&1

if [ $? -ne 0 ]; then
    echo "[$DATE] ALERTE : AIDE a détecté des modifications !" >> $LOG_FILE
    # Ici vous pourriez envoyer une alerte par email
    # echo "Alerte AIDE" | mail -s "Modifications détectées" admin@example.com
fi

echo "[$DATE] Vérification terminée" >> $LOG_FILE
```

**Explication** :
- Vérifie l'intégrité des fichiers système
- Enregistre les résultats dans un log
- Envoie une alerte si des modifications sont détectées

```bash
# Rendre exécutable
sudo chmod +x /usr/local/bin/aide-check.sh
```

**Ajouter au crontab** :

```bash
crontab -e
```

**Ajoutez** :

```cron
# Vérification AIDE quotidienne à 4h du matin
0 4 * * * /usr/local/bin/aide-check.sh
```

### 📌 ÉTAPE 4 : CONFIGURER LES SAUVEGARDES AUTOMATIQUES DES CONFIGURATIONS

**Pourquoi ?** Permet de restaurer rapidement la configuration en cas de problème.

```bash
# Créer un script pour sauvegarder les configurations
cd /srv/heptanet
nano backup-config.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

BACKUP_DIR="/srv/heptanet/backups/config"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# Sauvegarder les configurations importantes
tar -czf $BACKUP_DIR/config_$DATE.tar.gz \
    /srv/heptanet/nginx/nginx.conf \
    /srv/heptanet/docker-compose.yml \
    /srv/heptanet/postgres/postgres.conf \
    /etc/ssh/sshd_config \
    /etc/ufw/user.rules \
    /etc/fail2ban/jail.local

echo "✅ Configuration sauvegardée : $BACKUP_DIR/config_$DATE.tar.gz"

# Supprimer les anciennes sauvegardes (plus de 30 jours)
find $BACKUP_DIR -name "config_*.tar.gz" -mtime +30 -delete
```

**Explication** :
- **`tar -czf`** : Crée une archive compressée
- Sauvegarde toutes les configurations importantes
- Supprime les sauvegardes de plus de 30 jours

```bash
# Rendre exécutable
chmod +x backup-config.sh
```

**Ajouter au crontab** :

```bash
crontab -e
```

**Ajoutez** :

```cron
# Sauvegarde des configurations hebdomadaire (dimanche à 1h)
0 1 * * 0 /srv/heptanet/backup-config.sh
```

### 📌 ÉTAPE 5 : CONFIGURER LES MISE À JOUR AUTOMATIQUES DE SÉCURITÉ

**Pourquoi ?** Assure que le système reçoit automatiquement les correctifs de sécurité.

```bash
# Vérifier que unattended-upgrades est configuré
sudo dpkg-reconfigure -plow unattended-upgrades
```

**Explication** : Ouvre un menu interactif pour configurer les mises à jour automatiques.

**Sélectionnez "Yes"** pour activer les mises à jour automatiques.

```bash
# Vérifier la configuration
sudo nano /etc/apt/apt.conf.d/50unattended-upgrades
```

**Assurez-vous que ces lignes sont présentes** :

```conf
Unattended-Upgrade::Automatic-Reboot "false";
Unattended-Upgrade::Remove-Unused-Kernel-Packages "true";
Unattended-Upgrade::Remove-Unused-Dependencies "true";
```

**Explication** :
- **`Automatic-Reboot "false"`** : Ne redémarre pas automatiquement (vous pouvez le changer en "true" si vous préférez)
- **`Remove-Unused-Kernel-Packages "true"`** : Supprime les anciens noyaux
- **`Remove-Unused-Dependencies "true"`** : Supprime les dépendances inutilisées

```bash
# Vérifier le statut
sudo systemctl status unattended-upgrades
```

**Explication** : Vérifie que le service de mises à jour automatiques est actif.

### 📌 ÉTAPE 6 : CONFIGURER LES ALERTES PAR EMAIL (OPTIONNEL)

**Pourquoi ?** Recevez des notifications par email en cas de problème.

```bash
# Installer mailutils pour envoyer des emails
sudo apt install -y mailutils
```

**Explication** : Installe les outils nécessaires pour envoyer des emails depuis le serveur.

```bash
# Configurer Postfix (suivez les instructions)
sudo dpkg-reconfigure postfix
```

**Explication** : Configure Postfix (serveur de messagerie). Sélectionnez :
- **Internet Site** : Si vous avez un domaine
- **Local only** : Si vous n'avez pas de domaine

**Pour Gmail (recommandé)** :

```bash
# Installer s-nail (alternative plus simple)
sudo apt install -y s-nail
```

**Créer un script d'envoi d'email** :

```bash
nano /usr/local/bin/send-alert.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

TO_EMAIL="votre.email@gmail.com"
SUBJECT="$1"
BODY="$2"

echo "$BODY" | mail -s "$SUBJECT" "$TO_EMAIL"
```

**Explication** : Script simple pour envoyer des alertes par email.

```bash
# Rendre exécutable
sudo chmod +x /usr/local/bin/send-alert.sh
```

**Tester l'envoi d'email** :

```bash
# Tester
echo "Test d'alerte" | /usr/local/bin/send-alert.sh "Test" "Ceci est un test"
```

### 📌 ÉTAPE 7 : CONFIGURER LA SURVEILLANCE DES CONNEXIONS

**Pourquoi ?** Détecte les connexions suspectes et les tentatives d'intrusion.

```bash
# Installer et configurer netstat pour surveiller les connexions
sudo apt install -y net-tools
```

**Explication** : Installe les outils réseau nécessaires.

```bash
# Créer un script de surveillance
sudo nano /usr/local/bin/monitor-connections.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

LOG_FILE="/var/log/connection-monitor.log"
DATE=$(date '+%Y-%m-%d %H:%M:%S')

echo "[$DATE] Connexions actives:" >> $LOG_FILE
netstat -an | grep ESTABLISHED | wc -l >> $LOG_FILE

echo "[$DATE] Connexions par IP:" >> $LOG_FILE
netstat -an | grep ESTABLISHED | awk '{print $5}' | cut -d: -f1 | sort | uniq -c | sort -rn | head -10 >> $LOG_FILE

# Alerte si trop de connexions
CONN_COUNT=$(netstat -an | grep ESTABLISHED | wc -l)
if [ $CONN_COUNT -gt 100 ]; then
    echo "[$DATE] ALERTE : Nombre élevé de connexions ($CONN_COUNT)" >> $LOG_FILE
fi
```

**Explication** :
- Compte le nombre de connexions établies
- Liste les 10 IP avec le plus de connexions
- Alerte si le nombre de connexions est anormalement élevé

```bash
# Rendre exécutable
sudo chmod +x /usr/local/bin/monitor-connections.sh
```

**Ajouter au crontab** :

```bash
crontab -e
```

**Ajoutez** :

```cron
# Surveillance des connexions toutes les 5 minutes
*/5 * * * * /usr/local/bin/monitor-connections.sh
```

### 📌 ÉTAPE 8 : CONFIGURER LE LOGGING AVANCÉ

**Pourquoi ?** Améliore la traçabilité et facilite le débogage.

```bash
# Configurer rsyslog pour centraliser les logs
sudo nano /etc/rsyslog.conf
```

**Décommentez ces lignes** :

```conf
# Provides UDP syslog reception
module(load="imudp")
input(type="imudp" port="514")

# Provides TCP syslog reception
module(load="imtcp")
input(type="imtcp" port="514")
```

**Explication** : Active la réception de logs via UDP et TCP (pour les conteneurs Docker).

```bash
# Redémarrer rsyslog
sudo systemctl restart rsyslog
```

### 📌 ÉTAPE 9 : CONFIGURER LA PROTECTION CONTRE DDoS

**Pourquoi ?** Protège le serveur contre les attaques par déni de service.

**La configuration NGINX inclut déjà du rate limiting**, mais vous pouvez l'améliorer :

```bash
# Éditer la configuration NGINX
cd /srv/heptanet/nginx
nano nginx.conf
```

**Ajoutez ces règles de rate limiting supplémentaires** :

```nginx
# Rate limiting global
limit_req_zone $binary_remote_addr zone=global_limit:10m rate=30r/s;

# Dans le bloc server, ajoutez :
limit_req zone=global_limit burst=50 nodelay;
```

**Explication** :
- **`zone=global_limit`** : Zone de rate limiting globale
- **`rate=30r/s`** : 30 requêtes par seconde maximum
- **`burst=50`** : Autorise jusqu'à 50 requêtes en rafale

### 📌 ÉTAPE 10 : CONFIGURER LES BACKUPS DISTANTS

**Pourquoi ?** Protège contre la perte de données en cas de problème sur le serveur.

**Option 1 : Backup vers un autre serveur (SSH)** :

```bash
# Créer un script de backup distant
cd /srv/heptanet
nano backup-remote.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

REMOTE_HOST="autre-serveur.com"
REMOTE_USER="backup-user"
REMOTE_PATH="/backups/heptanet"
LOCAL_BACKUP="/srv/heptanet/backups"

# Créer le backup local d'abord
./backup-db.sh

# Copier le dernier backup vers le serveur distant
LATEST_BACKUP=$(ls -t $LOCAL_BACKUP/*.sql.gz | head -1)
scp $LATEST_BACKUP $REMOTE_USER@$REMOTE_HOST:$REMOTE_PATH/

echo "✅ Backup copié vers $REMOTE_HOST"
```

**Explication** :
- Crée d'abord un backup local
- Copie le dernier backup vers un serveur distant via SCP
- Utilise SSH pour la sécurité

**Option 2 : Backup vers un service cloud (S3, etc.)** :

```bash
# Installer AWS CLI (pour S3)
sudo apt install -y awscli
```

**Créer un script** :

```bash
nano backup-s3.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

S3_BUCKET="s3://votre-bucket/heptanet-backups"
LOCAL_BACKUP="/srv/heptanet/backups"

# Créer le backup local
./backup-db.sh

# Uploader vers S3
LATEST_BACKUP=$(ls -t $LOCAL_BACKUP/*.sql.gz | head -1)
aws s3 cp $LATEST_BACKUP $S3_BUCKET/

echo "✅ Backup uploadé vers S3"
```

---

## 2. MONITORING ET ALERTES

### 📌 ÉTAPE 1 : INSTALLER ET CONFIGURER UN MONITORING AVANCÉ

**Option 1 : Utiliser Prometheus + Grafana (avancé)** :

```bash
# Créer un fichier docker-compose pour le monitoring
cd /srv/heptanet
nano docker-compose.monitoring.yml
```

**Collez ce contenu** :

```yaml
version: '3.9'

services:
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    ports:
      - "9090:9090"
    networks:
      - heptanet_network

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    volumes:
      - grafana_data:/var/lib/grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    networks:
      - heptanet_network

volumes:
  prometheus_data:
  grafana_data:

networks:
  heptanet_network:
    external: true
```

**Explication** :
- **Prometheus** : Collecte les métriques
- **Grafana** : Visualise les métriques avec des graphiques

**Option 2 : Monitoring simple avec des scripts** :

```bash
# Créer un script de monitoring complet
cd /srv/heptanet
nano monitor-advanced.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

LOG_FILE="/srv/heptanet/logs/monitoring.log"
DATE=$(date '+%Y-%m-%d %H:%M:%S')

log() {
    echo "[$DATE] $1" | tee -a $LOG_FILE
}

log "=== Début du monitoring ==="

# CPU
CPU_USAGE=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1)
log "CPU Usage: ${CPU_USAGE}%"

# RAM
RAM_USAGE=$(free | grep Mem | awk '{printf "%.2f", $3/$2 * 100.0}')
log "RAM Usage: ${RAM_USAGE}%"

# Disque
DISK_USAGE=$(df -h /srv | awk 'NR==2 {print $5}')
log "Disk Usage: $DISK_USAGE"

# Containers
CONTAINERS_UP=$(docker compose ps | grep -c "Up")
log "Containers running: $CONTAINERS_UP/3"

# API Health
if curl -s http://localhost:5106/health > /dev/null; then
    log "API Status: ✅ Healthy"
else
    log "API Status: ❌ Unhealthy"
fi

log "=== Fin du monitoring ==="
```

### 📌 ÉTAPE 2 : CONFIGURER LES ALERTES AUTOMATIQUES

```bash
# Créer un script d'alerte
cd /srv/heptanet
nano alert.sh
```

**Collez ce contenu** :

```bash
#!/bin/bash

ALERT_EMAIL="votre.email@gmail.com"
THRESHOLD_CPU=80
THRESHOLD_RAM=85
THRESHOLD_DISK=90

# Vérifier CPU
CPU_USAGE=$(top -bn1 | grep "Cpu(s)" | awk '{print $2}' | cut -d'%' -f1 | cut -d'.' -f1)
if [ $CPU_USAGE -gt $THRESHOLD_CPU ]; then
    echo "ALERTE: CPU usage à ${CPU_USAGE}%" | mail -s "Alerte CPU" $ALERT_EMAIL
fi

# Vérifier RAM
RAM_USAGE=$(free | grep Mem | awk '{printf "%.0f", $3/$2 * 100.0}')
if [ $RAM_USAGE -gt $THRESHOLD_RAM ]; then
    echo "ALERTE: RAM usage à ${RAM_USAGE}%" | mail -s "Alerte RAM" $ALERT_EMAIL
fi

# Vérifier Disque
DISK_USAGE=$(df -h /srv | awk 'NR==2 {print $5}' | sed 's/%//')
if [ $DISK_USAGE -gt $THRESHOLD_DISK ]; then
    echo "ALERTE: Disk usage à ${DISK_USAGE}%" | mail -s "Alerte Disque" $ALERT_EMAIL
fi
```

**Ajouter au crontab** :

```bash
crontab -e
```

**Ajoutez** :

```cron
# Vérification des alertes toutes les 15 minutes
*/15 * * * * /srv/heptanet/alert.sh
```

---

## 3. TROUBLESHOOTING

### 📌 PROBLÈME : Les containers ne démarrent pas

**Symptômes** : Les conteneurs ne démarrent pas ou redémarrent en boucle.

**Diagnostic** :

```bash
# Vérifier les logs
docker compose logs
```

**Explication** : Affiche les logs de tous les services pour identifier l'erreur.

```bash
# Vérifier la configuration
docker compose config
```

**Explication** : Vérifie la syntaxe du fichier `docker-compose.yml`.

```bash
# Vérifier les erreurs spécifiques
docker compose logs backend | grep -i error
docker compose logs postgres | grep -i error
```

**Explication** : Recherche les erreurs dans les logs d'un service spécifique.

**Solutions** :

```bash
# Redémarrer tous les services
docker compose down
docker compose up -d
```

**Explication** : Arrête et redémarre tous les services.

```bash
# Rebuild les images
docker compose build --no-cache
docker compose up -d
```

**Explication** : Reconstruit les images depuis zéro et redémarre.

### 📌 PROBLÈME : Le backend ne peut pas se connecter à PostgreSQL

**Symptômes** : Le backend démarre mais ne peut pas se connecter à la base de données.

**Diagnostic** :

```bash
# Vérifier que PostgreSQL est démarré
docker compose ps postgres
```

**Explication** : Vérifie l'état du conteneur PostgreSQL.

```bash
# Vérifier les logs PostgreSQL
docker compose logs postgres
```

**Explication** : Affiche les logs PostgreSQL pour identifier le problème.

```bash
# Tester la connexion depuis le backend
docker compose exec backend ping postgres
```

**Explication** : Teste la connectivité réseau entre les conteneurs.

```bash
# Vérifier la chaîne de connexion
cat backend/HeptaNet.API/appsettings.Production.json | grep ConnectionString
```

**Explication** : Vérifie que la chaîne de connexion est correcte.

```bash
# Vérifier que le mot de passe est correct dans .env
cat .env
```

**Explication** : Vérifie que le mot de passe PostgreSQL dans `.env` correspond à celui dans `appsettings.Production.json`.

**Solutions** :

```bash
# Redémarrer PostgreSQL
docker compose restart postgres
```

**Explication** : Redémarre le conteneur PostgreSQL.

```bash
# Vérifier que PostgreSQL accepte les connexions
docker compose exec postgres psql -U heptanet_user -d heptanet_db -c "SELECT 1;"
```

**Explication** : Teste la connexion directement à PostgreSQL.

### 📌 PROBLÈME : NGINX ne redirige pas vers le backend

**Symptômes** : NGINX répond mais ne transmet pas les requêtes au backend.

**Diagnostic** :

```bash
# Vérifier la configuration NGINX
docker compose exec nginx nginx -t
```

**Explication** : Teste la syntaxe de la configuration NGINX.

```bash
# Vérifier les logs NGINX
docker compose logs nginx
```

**Explication** : Affiche les logs NGINX pour identifier les erreurs.

```bash
# Vérifier que le backend est accessible depuis NGINX
docker compose exec nginx ping backend
```

**Explication** : Teste la connectivité réseau entre NGINX et le backend.

**Solutions** :

```bash
# Redémarrer NGINX
docker compose restart nginx
```

**Explication** : Redémarre le conteneur NGINX.

```bash
# Vérifier que le backend répond
curl http://localhost:5106
```

**Explication** : Teste directement le backend sans passer par NGINX.

### 📌 PROBLÈME : Les migrations échouent

**Symptômes** : Les migrations Entity Framework ne s'appliquent pas.

**Diagnostic** :

```bash
# Vérifier que PostgreSQL est accessible
docker compose exec postgres psql -U heptanet_user -d heptanet_db -c "SELECT 1;"
```

**Explication** : Teste la connexion à PostgreSQL.

```bash
# Vérifier les migrations existantes
docker compose exec backend dotnet ef migrations list \
  --project /src/HeptaNet.Infrastructure/HeptaNet.Infrastructure.csproj \
  --startup-project /src/HeptaNet.API/HeptaNet.API.csproj
```

**Explication** : Liste toutes les migrations disponibles.

**Solutions** :

```bash
# Appliquer les migrations manuellement
docker compose exec backend dotnet ef database update \
  --project /src/HeptaNet.Infrastructure/HeptaNet.Infrastructure.csproj \
  --startup-project /src/HeptaNet.API/HeptaNet.API.csproj
```

**Explication** : Applique les migrations manuellement.

```bash
# Si les migrations sont corrompues, les supprimer et recréer
# ATTENTION : Cela supprimera toutes les données !
docker compose exec postgres psql -U heptanet_user -d heptanet_db -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
```

**Explication** : **DANGEREUX** : Supprime et recrée le schéma de la base de données. Utilisez uniquement en développement !

### 📌 PROBLÈME : Le déploiement GitHub Actions échoue

**Symptômes** : Le workflow GitHub Actions ne se termine pas ou échoue.

**Diagnostic** :

1. **Vérifier les secrets GitHub** :
   - Allez dans **Settings** → **Secrets and variables** → **Actions**
   - Vérifiez que tous les secrets sont définis

2. **Vérifier la clé SSH** :
   ```bash
   # Sur le VPS, vérifier que la clé est dans authorized_keys
   cat ~/.ssh/authorized_keys
   ```

3. **Tester la connexion SSH manuellement** :
   ```bash
   # Depuis votre machine locale
   ssh -i ~/.ssh/github_actions_key heptanet@51.83.72.29
   ```

**Solutions** :

```bash
# Vérifier que Git est correctement configuré sur le VPS
cd /srv/heptanet/backend
git remote -v
```

**Explication** : Vérifie que Git pointe vers le bon repository.

```bash
# Tester le pull manuellement
git pull origin main
```

**Explication** : Teste que Git peut récupérer les modifications depuis GitHub.

### 📌 PROBLÈME : Erreurs de permissions

**Symptômes** : Les fichiers ne peuvent pas être créés ou modifiés.

**Diagnostic** :

```bash
# Vérifier les permissions des fichiers
ls -la /srv/heptanet
```

**Explication** : Affiche les permissions de tous les fichiers.

**Solutions** :

```bash
# Corriger les permissions si nécessaire
sudo chown -R $USER:$USER /srv/heptanet
chmod 600 /srv/heptanet/.env
chmod 600 /srv/heptanet/backend/HeptaNet.API/appsettings.Production.json
```

**Explication** :
- `chown` : Change le propriétaire des fichiers
- `chmod 600` : Donne les permissions lecture/écriture au propriétaire uniquement

### 📌 PROBLÈME : Le firewall bloque les connexions

**Symptômes** : Impossible de se connecter au serveur ou à l'API depuis l'extérieur.

**Diagnostic** :

```bash
# Vérifier le statut du firewall
sudo ufw status verbose
```

**Explication** : Affiche l'état du firewall et toutes les règles.

```bash
# Vérifier les règles
sudo ufw status numbered
```

**Explication** : Affiche les règles avec des numéros pour faciliter la gestion.

```bash
# Vérifier les logs du firewall
sudo tail -f /var/log/ufw.log
```

**Explication** : Affiche les logs du firewall en temps réel.

**Solutions** :

```bash
# Autoriser un port si nécessaire
sudo ufw allow 80/tcp
```

**Explication** : Autorise le port 80 (HTTP).

```bash
# Supprimer une règle si nécessaire
sudo ufw delete [numéro]
```

**Explication** : Supprime une règle spécifique (utilisez le numéro de la règle).

### 📌 PROBLÈME : L'API ne répond pas

**Symptômes** : L'API ne répond pas aux requêtes.

**Diagnostic** :

```bash
# Vérifier que le backend est en cours d'exécution
docker compose ps backend
```

**Explication** : Vérifie l'état du conteneur backend.

```bash
# Vérifier les logs du backend
docker compose logs --tail=50 backend
```

**Explication** : Affiche les 50 dernières lignes des logs.

```bash
# Tester l'API directement
curl -v http://localhost:5106
```

**Explication** : Teste l'API directement sans passer par NGINX.

**Solutions** :

```bash
# Redémarrer le backend
docker compose restart backend
```

**Explication** : Redémarre le conteneur backend.

```bash
# Rebuild et redémarrer
docker compose build backend
docker compose up -d --no-deps backend
```

**Explication** : Reconstruit l'image et redémarre le backend.

---

## 4. COMMANDES DE RÉFÉRENCE

### 📌 Commandes Docker essentielles

```bash
# Démarrer tous les services
cd /srv/heptanet && docker compose up -d

# Arrêter tous les services
docker compose down

# Voir les logs
docker compose logs -f

# Rebuild et redémarrer le backend
docker compose build backend
docker compose up -d --no-deps backend

# Voir l'état des services
docker compose ps

# Voir l'utilisation des ressources
docker stats

# Nettoyer les ressources inutilisées
docker system prune -f
```

### 📌 Commandes de maintenance

```bash
# Backup de la base de données
./backup-db.sh

# Restauration
./restore-db.sh /srv/heptanet/backups/heptanet_db_YYYYMMDD_HHMMSS.sql.gz

# Monitoring
./monitor.sh

# Maintenance
./maintenance.sh

# Vérification de santé
./health-check.sh
```

### 📌 Commandes de sécurité

```bash
# Vérifier Fail2Ban
sudo fail2ban-client status sshd

# Vérifier le firewall
sudo ufw status verbose

# Vérifier les connexions
sudo netstat -tulpn | grep LISTEN

# Vérifier les logs de sécurité
sudo tail -f /var/log/auth.log
```

### 📌 Commandes de diagnostic

```bash
# Vérifier l'espace disque
df -h

# Vérifier la RAM
free -h

# Vérifier les processus
top

# Vérifier les connexions réseau
netstat -an | grep ESTABLISHED

# Vérifier les erreurs système
dmesg | tail -20
```

---

## 5. CHECKLIST DE SÉCURITÉ COMPLÈTE

### 🔒 Sécurité Système

- [ ] Utilisateur non-root créé et configuré
- [ ] Authentification SSH par clé configurée
- [ ] Authentification par mot de passe SSH désactivée
- [ ] Port SSH personnalisé configuré (optionnel mais recommandé)
- [ ] Fail2Ban installé et configuré
- [ ] Firewall UFW activé et configuré
- [ ] Services inutiles désactivés
- [ ] Mises à jour automatiques de sécurité activées
- [ ] Limites de ressources configurées

### 🔒 Sécurité Docker

- [ ] Docker configuré avec no-new-privileges
- [ ] Containers avec utilisateur non-root
- [ ] Containers avec read_only filesystem
- [ ] Ports PostgreSQL et Backend exposés uniquement sur localhost
- [ ] Logs Docker configurés avec rotation
- [ ] Limites de ressources configurées

### 🔒 Sécurité Application

- [ ] Mots de passe forts générés (PostgreSQL, JWT)
- [ ] Fichiers sensibles (.env, appsettings.Production.json) avec permissions 600
- [ ] CORS configuré avec les origines autorisées uniquement
- [ ] Rate limiting configuré dans NGINX
- [ ] Headers de sécurité configurés dans NGINX
- [ ] Secrets stockés dans des variables d'environnement

### 🔒 Sécurité Réseau

- [ ] Firewall configuré et actif
- [ ] Seuls les ports nécessaires sont ouverts (22, 80)
- [ ] Rate limiting configuré
- [ ] Protection contre DDoS configurée

### 🔒 Sauvegardes

- [ ] Backups automatiques configurés
- [ ] Backups testés et restaurés avec succès
- [ ] Backups distants configurés (optionnel)
- [ ] Sauvegarde des configurations

### 🔒 Monitoring

- [ ] Scripts de monitoring créés
- [ ] Alertes configurées
- [ ] Logs configurés avec rotation
- [ ] Surveillance des connexions active

### 🔒 Maintenance

- [ ] Scripts de maintenance créés
- [ ] Cron jobs configurés
- [ ] Documentation à jour
- [ ] Procédures de récupération testées

---

## 📌 RÉSUMÉ DES BONNES PRATIQUES

1. **Toujours utiliser un utilisateur non-root** pour les opérations quotidiennes
2. **Authentification SSH par clé uniquement** (désactiver les mots de passe)
3. **Firewall actif** avec uniquement les ports nécessaires ouverts
4. **Mots de passe forts** pour tous les services
5. **Backups automatiques** testés régulièrement
6. **Monitoring actif** avec alertes configurées
7. **Mises à jour de sécurité** automatiques
8. **Logs configurés** avec rotation pour éviter le remplissage du disque
9. **Containers sécurisés** avec no-new-privileges et read_only
10. **Documentation à jour** pour faciliter la maintenance

---

## 🆘 EN CAS D'URGENCE

### Si vous êtes bloqué hors du serveur :

1. Contactez votre fournisseur VPS pour accéder à la console
2. Vérifiez que le firewall n'a pas bloqué votre IP
3. Vérifiez que le service SSH est actif

### Si l'API ne fonctionne plus :

1. Vérifiez les logs : `docker compose logs backend`
2. Vérifiez l'état des services : `docker compose ps`
3. Redémarrez les services : `docker compose restart`

### Si la base de données est corrompue :

1. Restaurez depuis un backup : `./restore-db.sh [backup_file]`
2. Vérifiez les logs PostgreSQL : `docker compose logs postgres`
3. Vérifiez l'espace disque : `df -h`

---

**🎯 Documentation complète terminée !**

Ces trois fichiers couvrent :
- ✅ Installation et configuration complète
- ✅ Déploiement et CI/CD automatique
- ✅ Sécurité avancée et troubleshooting

Votre serveur VPS est maintenant configuré de manière sécurisée et prêt pour la production !


