# Architecture Clean .NET - HeptaNet

## Vue d'ensemble

L'architecture Clean (Clean Architecture) est un modèle architectural qui organise le code en couches concentriques, où chaque couche a des responsabilités spécifiques et des dépendances bien définies. Cette approche garantit la séparation des préoccupations, la testabilité et la maintenabilité du code.

## Structure du projet

Notre solution HeptaNet suit l'architecture Clean avec quatre projets principaux :

```
HeptaNet.sln
├── HeptaNet.API/           # Couche de présentation (Web API)
├── HeptaNet.Application/   # Couche d'application (Services métier)
├── HeptaNet.Domain/        # Couche domaine (Entités et règles métier)
└── HeptaNet.Infrastructure/ # Couche infrastructure (Accès aux données)
```

## Couches de l'architecture

### 1. HeptaNet.Domain (Couche Domaine)
**Responsabilité** : Contient la logique métier pure et les entités centrales.

#### Structure des dossiers :
- **Base/** : Contient l'entité de base `BaseEntity`
- **Models/** : Contient tous les modèles qui dérivent de `BaseEntity`
- **Dto/** : Contient tous les objets de transfert de données (DTO)
- **Request/** : Contient tous les records utilisés dans les requêtes (optionnel)
- **Query/** : Contient des objets pour mapper directement les résultats de requêtes SQL (optionnel)

#### Caractéristiques :
- Aucune dépendance externe
- Contient les règles métier fondamentales
- Entités indépendantes de la technologie

### 2. HeptaNet.Application (Couche Application)
**Responsabilité** : Orchestre les cas d'usage et coordonne les opérations métier.

#### Structure des dossiers :
- **Base/** : Contient le `BaseService` et son interface pour définir les méthodes communes
- **Services/** : Contient tous les services métier

#### Caractéristiques :
- Dépend uniquement du projet Domain
- Implémente les cas d'usage
- Coordonne les opérations entre les entités du domaine

### 3. HeptaNet.Infrastructure (Couche Infrastructure)
**Responsabilité** : Gère l'accès aux données et les services externes.

#### Structure des dossiers :
- **Base/** : Contient le `BaseRepository` et son interface pour définir les méthodes communes
- **Database/** : Contient le `CoreDbContext` et les configurations
  - **Configurations/** : Configurations Entity Framework pour une meilleure lisibilité
- **Repository/** : Contient tous les repositories
- **Migrations/** : Créé automatiquement par Entity Framework lors de la première migration

#### Caractéristiques :
- Implémente les interfaces définies dans Application
- Gère la persistance des données
- Peut être remplacé sans affecter les autres couches

### 4. HeptaNet.API (Couche Présentation)
**Responsabilité** : Expose les fonctionnalités via des endpoints HTTP.

#### Caractéristiques :
- Contient les contrôleurs Web API
- Configure les services et l'injection de dépendances
- Point d'entrée de l'application

## Principes de l'architecture Clean

### 1. Dépendances vers l'intérieur
Les dépendances pointent toujours vers le centre (Domain), jamais vers l'extérieur.

### 2. Séparation des préoccupations
Chaque couche a une responsabilité spécifique et bien définie.

### 3. Inversion de dépendance
Les couches externes dépendent des abstractions définies dans les couches internes.

### 4. Indépendance des frameworks
Le domaine métier ne dépend d'aucun framework externe.

## Avantages

- **Testabilité** : Chaque couche peut être testée indépendamment
- **Maintenabilité** : Modifications isolées dans chaque couche
- **Flexibilité** : Possibilité de changer l'implémentation sans affecter les autres couches
- **Évolutivité** : Structure claire pour l'ajout de nouvelles fonctionnalités

## Flux de données

```
API Controller → Application Service → Domain Entity
                     ↓
              Infrastructure Repository → Database
```

## Bonnes pratiques

1. **Domain** : Garder la logique métier pure, sans dépendances externes
2. **Application** : Utiliser des interfaces pour définir les contrats
3. **Infrastructure** : Implémenter les interfaces définies dans Application
4. **API** : Garder les contrôleurs légers, déléguer la logique aux services

## Conclusion

Cette architecture Clean garantit une séparation claire des responsabilités, facilitant la maintenance, les tests et l'évolution de l'application HeptaNet. Chaque couche a un rôle précis et les dépendances sont contrôlées pour maintenir la cohérence et la qualité du code.
