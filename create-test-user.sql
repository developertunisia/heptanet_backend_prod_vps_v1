-- Script pour créer un utilisateur de test dans PostgreSQL
-- À exécuter dans Adminer : http://localhost:8082
-- Connexion : db / hepta / HEPTAhepta123 / mydatabase

-- 1. D'abord, créer un rôle Admin si nécessaire
INSERT INTO "Roles" ("Name", "Description") 
VALUES ('Admin', 'Administrateur du système')
ON CONFLICT DO NOTHING;

-- 2. Récupérer l'ID du rôle (remplacez 1 par l'ID réel si différent)
-- SELECT "RoleId" FROM "Roles" WHERE "Name" = 'Admin';

-- 3. Créer un utilisateur de test
-- ⚠️ ATTENTION : Le hash du mot de passe est généré automatiquement par ASP.NET Identity
-- Pour créer un utilisateur fonctionnel, utilisez l'API endpoint POST /api/users/register
-- ou utilisez le code C# pour hash correctement le mot de passe

-- Pour tester, utilisez directement l'API avec une commande curl ou via Swagger

-- Exemple pour créer un utilisateur via l'API :
-- curl -X POST http://localhost:5106/api/users/register \
--   -H "Content-Type: application/json" \
--   -d '{"firstName":"Admin","lastName":"User","email":"admin@hepta.net","password":"Admin123!","confirmPassword":"Admin123!","roleId":1}'

