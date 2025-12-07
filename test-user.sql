-- Script pour créer un utilisateur de test dans la base de données
-- À exécuter dans Adminer : http://localhost:8082

-- 1. D'abord, créer un rôle si nécessaire
INSERT INTO "Roles" ("Name", "Description") 
VALUES ('Admin', 'Administrateur du système')
ON CONFLICT DO NOTHING;

-- 2. Récupérer l'ID du rôle Admin
-- Note : Vous devez remplacer 1 par l'ID réel du rôle dans votre base

-- 3. Créer un utilisateur de test
-- Remplacez 'PBKDF2...' par un hash de mot de passe valide
-- Pour générer un hash valide, utilisez le code C# ou laissez l'application le faire

-- Option A : Créer via l'application (RECOMMANDÉ)
-- Utilisez l'endpoint POST /api/users/register via Swagger

-- Option B : Créer manuellement (NON RECOMMANDÉ)
-- Cela nécessite de hasher le mot de passe avec ASP.NET Identity
-- INSERT INTO "AspNetUsers" (
--     "Id", "FirstName", "LastName", "Email", "UserName", "NormalizedEmail", 
--     "NormalizedUserName", "EmailConfirmed", "PasswordHash", "SecurityStamp",
--     "RoleId", "IsBlacklisted", "CreatedAt", "AccessFailedCount", "LockoutEnabled"
-- ) VALUES (
--     1, 'Admin', 'User', 'admin@hepta.net', 'admin@hepta.net', 'ADMIN@HEPTA.NET',
--     'ADMIN@HEPTA.NET', true, 'PBKDF2_HASH_HERE', 'SECURITY_STAMP_HERE',
--     1, false, NOW(), 0, false
-- );

-- L'utilisateur sera créé via l'interface Swagger de l'API

