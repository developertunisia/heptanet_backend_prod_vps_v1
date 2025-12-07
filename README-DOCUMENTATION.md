# 📚 Documentation Complète - Déploiement HeptaNet sur VPS

## 🎯 Vue d'ensemble

Cette documentation complète vous guide pas à pas pour déployer votre backend HeptaNet (.NET 9) sur un VPS Ubuntu 22.04 avec Docker, PostgreSQL, NGINX et une configuration sécurisée.

**Configuration actuelle :**
- **VPS IP** : `51.83.72.29`
- **URL API** : `http://51.83.72.29`
- **Architecture** : Docker Compose avec 3 services (Backend, PostgreSQL, NGINX)

---

## 📖 Structure de la Documentation

La documentation est divisée en **3 fichiers** pour faciliter la navigation :

### 1. 📘 [01-Installation-et-Configuration.md](01-Installation-et-Configuration.md)
**Contenu :**
- Vue d'ensemble et prérequis
- Sécurité initiale du serveur (CRITIQUE)
- Installation de Docker et Docker Compose
- Configuration de l'environnement
- Configuration du backend .NET 9
- Configuration PostgreSQL sécurisée
- Configuration NGINX (Reverse Proxy)
- Configuration Docker Compose
- Configuration du Firewall

**À lire en premier** pour configurer votre serveur de base.

### 2. 📘 [02-Deploiement-et-CICD.md](02-Deploiement-et-CICD.md)
**Contenu :**
- Déploiement initial
- Configuration GitHub Actions (CI/CD automatique)
- Scripts de maintenance et backup
- Monitoring et logs

**À lire ensuite** pour déployer votre application et automatiser les mises à jour.

### 3. 📘 [03-Securite-et-Monitoring.md](03-Securite-et-Monitoring.md)
**Contenu :**
- Sécurité avancée
- Monitoring et alertes
- Troubleshooting complet
- Commandes de référence
- Checklist de sécurité complète

**À consulter** pour renforcer la sécurité et résoudre les problèmes.

---

## 🚀 Démarrage Rapide

### Étape 1 : Lire la documentation dans l'ordre

1. Commencez par **01-Installation-et-Configuration.md**
2. Suivez **02-Deploiement-et-CICD.md** pour le déploiement
3. Consultez **03-Securite-et-Monitoring.md** pour la sécurité avancée

### Étape 2 : Préparer votre environnement

**Sur votre machine locale :**
- Avoir un accès SSH au VPS
- Avoir un compte GitHub avec le repository du projet
- Avoir une clé SSH générée

**Sur le VPS :**
- Ubuntu 22.04 LTS installé
- Accès root ou sudo
- Au moins 4 GB de RAM recommandé

### Étape 3 : Suivre les étapes dans l'ordre

Chaque fichier contient des étapes numérotées avec :
- ✅ **Commandes SSH** à exécuter
- ✅ **Explications détaillées** de chaque commande
- ✅ **Vérifications** à effectuer
- ✅ **Points de sécurité** importants

---

## 🔒 Points de Sécurité Critiques

⚠️ **NE SAUTEZ PAS CES ÉTAPES :**

1. **Créer un utilisateur non-root** (Section 4, Étape 2)
2. **Configurer l'authentification SSH par clé** (Section 4, Étape 3)
3. **Désactiver l'authentification par mot de passe SSH** (Section 4, Étape 4)
4. **Installer et configurer Fail2Ban** (Section 4, Étape 5)
5. **Configurer le Firewall UFW** (Section 4, Étape 6 et Section 13)

---

## 📋 Checklist Globale

### Avant de commencer
- [ ] VPS Ubuntu 22.04 configuré
- [ ] Accès SSH fonctionnel
- [ ] Repository GitHub prêt
- [ ] Documentation lue en entier

### Installation
- [ ] Sécurité initiale configurée
- [ ] Docker installé et configuré
- [ ] Structure de répertoires créée
- [ ] Code source cloné depuis GitHub

### Configuration
- [ ] Backend .NET configuré
- [ ] PostgreSQL configuré
- [ ] NGINX configuré
- [ ] Docker Compose configuré
- [ ] Firewall configuré

### Déploiement
- [ ] Services démarrés avec succès
- [ ] Migrations appliquées
- [ ] API accessible depuis l'extérieur
- [ ] GitHub Actions configuré
- [ ] Déploiement automatique testé

### Sécurité
- [ ] Tous les points de sécurité critiques appliqués
- [ ] Backups automatiques configurés
- [ ] Monitoring configuré
- [ ] Alertes configurées

---

## 🛠️ Commandes Essentielles

### Démarrer les services
```bash
cd /srv/heptanet
docker compose up -d
```

### Voir les logs
```bash
docker compose logs -f
```

### Backup de la base de données
```bash
cd /srv/heptanet
./backup-db.sh
```

### Monitoring
```bash
cd /srv/heptanet
./monitor.sh
```

### Redémarrer le backend
```bash
cd /srv/heptanet
docker compose restart backend
```

---

## 📞 Support et Troubleshooting

### En cas de problème

1. **Consultez la section Troubleshooting** dans `03-Securite-et-Monitoring.md`
2. **Vérifiez les logs** : `docker compose logs [service]`
3. **Vérifiez l'état des services** : `docker compose ps`
4. **Consultez les commandes de référence** dans `03-Securite-et-Monitoring.md`

### Problèmes courants

- **Containers ne démarrent pas** → Voir Section 3.1 dans `03-Securite-et-Monitoring.md`
- **Backend ne se connecte pas à PostgreSQL** → Voir Section 3.2
- **NGINX ne redirige pas** → Voir Section 3.3
- **Migrations échouent** → Voir Section 3.4
- **GitHub Actions échoue** → Voir Section 3.5

---

## 📝 Notes Importantes

### Configuration IP uniquement

Cette documentation est configurée pour utiliser uniquement une IP (`51.83.72.29`) sans nom de domaine. Cela signifie :
- ✅ Pas de SSL/HTTPS (Let's Encrypt nécessite un domaine)
- ✅ Communication en HTTP uniquement
- ⚠️ Non recommandé pour la production avec données sensibles

**Pour améliorer la sécurité :**
- Obtenez un nom de domaine gratuit (Freenom, DuckDNS, etc.)
- Configurez Let's Encrypt pour HTTPS
- Modifiez la configuration NGINX pour utiliser SSL

### Frontend Configuration

Dans votre frontend, utilisez cette URL pour l'API :
```javascript
const API_URL = 'http://51.83.72.29';
```

### Mots de passe

- **Générez des mots de passe forts** avec : `openssl rand -base64 32`
- **Ne commitez JAMAIS** les fichiers `.env` ou `appsettings.Production.json`
- **Utilisez des secrets différents** pour chaque environnement

---

## 🔄 Mises à Jour

### Mettre à jour le code

Le déploiement automatique via GitHub Actions met à jour le code à chaque push sur `main`. Sinon, manuellement :

```bash
cd /srv/heptanet/backend
git pull origin main
cd /srv/heptanet
docker compose build backend
docker compose up -d --no-deps backend
```

### Mettre à jour Docker

```bash
sudo apt update
sudo apt upgrade docker-ce docker-ce-cli containerd.io
```

### Mettre à jour les images Docker

```bash
cd /srv/heptanet
docker compose pull
docker compose up -d
```

---

## ✅ Vérification Finale

Avant de considérer le déploiement terminé, vérifiez :

- [ ] L'API répond : `curl http://51.83.72.29/api/users`
- [ ] Tous les services sont "Up" : `docker compose ps`
- [ ] Les logs ne montrent pas d'erreurs : `docker compose logs`
- [ ] Le firewall est actif : `sudo ufw status`
- [ ] Fail2Ban est actif : `sudo fail2ban-client status sshd`
- [ ] Les backups fonctionnent : `./backup-db.sh`
- [ ] GitHub Actions fonctionne : Testez un push sur `main`

---

## 📚 Ressources Supplémentaires

- [Documentation Docker](https://docs.docker.com/)
- [Documentation Docker Compose](https://docs.docker.com/compose/)
- [Documentation NGINX](https://nginx.org/en/docs/)
- [Documentation PostgreSQL](https://www.postgresql.org/docs/)
- [Documentation .NET](https://docs.microsoft.com/dotnet/)

---

## 🎯 Prochaines Étapes

Une fois le déploiement terminé :

1. **Configurer un nom de domaine** (pour HTTPS)
2. **Mettre en place un monitoring avancé** (Prometheus + Grafana)
3. **Configurer des backups distants** (S3, autre serveur)
4. **Mettre en place des alertes par email**
5. **Optimiser les performances** (cache Redis, CDN, etc.)

---

**📌 Bon déploiement !**

Si vous avez des questions ou rencontrez des problèmes, consultez la section Troubleshooting dans `03-Securite-et-Monitoring.md`.


