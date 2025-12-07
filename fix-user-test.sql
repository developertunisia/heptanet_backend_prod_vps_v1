-- Script pour créer ou réinitialiser un utilisateur de test
-- À exécuter dans Adminer : http://localhost:8082

-- 1. Voir tous les utilisateurs existants
SELECT "Id", "Email", "FirstName", "LastName", "RoleId", "IsBlacklisted" 
FROM "AspNetUsers";

-- 2. Supprimer l'utilisateur existant si nécessaire
-- DELETE FROM "AspNetUsers" WHERE "Email" = 'admin@hepta.net';

-- OU créer un utilisateur avec un email différent pour tester
-- (utilisez l'API pour créer l'utilisateur, pas SQL direct)

-- 3. Vérifier les rôles existants
SELECT "RoleId", "Name", "Description" FROM "Roles";





