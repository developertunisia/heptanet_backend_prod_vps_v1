# Documentation Technique - Implémentation des Messages Vocaux

## 📋 Table des Matières

1. [Vue d'ensemble](#vue-densemble)
2. [Objectifs de la tâche](#objectifs-de-la-tâche)
3. [Architecture et Principes](#architecture-et-principes)
4. [Modifications Détaillées](#modifications-détaillées)
5. [Flux de Données](#flux-de-données)
6. [Endpoints API](#endpoints-api)
7. [Gestion des Erreurs](#gestion-des-erreurs)
8. [Tests et Validation](#tests-et-validation)

---

## 1. Vue d'ensemble

### Contexte
Cette documentation décrit l'implémentation complète de la fonctionnalité de messages vocaux dans l'application HeptaNet, ainsi que l'amélioration de la gestion des erreurs pour retourner des réponses JSON claires et structurées.

### Problèmes Résolus
1. **Erreurs 500 sans détails** : Les erreurs retournaient des pages HTML au lieu de JSON structuré
2. **Absence de messages vocaux** : Impossibilité d'envoyer et de lire des messages audio
3. **URLs incomplètes** : Les URLs des fichiers audio n'étaient pas complètes dans les réponses API

---

## 2. Objectifs de la Tâche

### Objectifs Principaux
1. ✅ Implémenter l'envoi de messages vocaux avec validation et stockage
2. ✅ Extraire les métadonnées audio (durée) automatiquement
3. ✅ Retourner les messages avec URLs complètes des fichiers audio
4. ✅ Créer un endpoint pour lire les messages vocaux
5. ✅ Améliorer la gestion d'erreurs avec réponses JSON structurées
6. ✅ Respecter l'architecture Clean Architecture

---

## 3. Architecture et Principes

### Structure Clean Architecture Respectée

```
HeptaNet.Domain/          → Couche Domaine (Entités, DTOs, Requests)
HeptaNet.Application/     → Couche Application (Interfaces, Services)
HeptaNet.Infrastructure/   → Couche Infrastructure (Implémentations)
HeptaNet.API/             → Couche Présentation (Controllers, Middleware)
```

### Principes Appliqués
- **Séparation des responsabilités** : Chaque couche a un rôle précis
- **Dépendances vers l'intérieur** : Les couches externes dépendent des couches internes
- **Inversion de dépendances** : Utilisation d'interfaces dans Application, implémentations dans Infrastructure
- **Indépendance des frameworks** : Le Domain ne dépend pas d'ASP.NET Core

---

## 4. Modifications Détaillées

### 4.1. Création des Services de Stockage et Audio

#### 4.1.1. Interface IFileStorageService
**Fichier :** `HeptaNet.Application/Services/FileStorage/IFileStorageService.cs`

**Création :** Nouveau fichier

**Description :** Interface définissant le contrat pour le stockage de fichiers.

**Code :**
```csharp
public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folder, int userId);
    Task<bool> DeleteFileAsync(string fileUrl);
    string GetFileUrl(string fileName, string folder);
    Task<FileInfo> GetFileInfoAsync(string fileUrl);
}

public class FileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? DurationSeconds { get; set; }
}
```

**Explication :**
- **SaveFileAsync** : Sauvegarde un fichier dans un dossier spécifique pour un utilisateur
- **DeleteFileAsync** : Supprime un fichier par son URL
- **GetFileUrl** : Génère l'URL relative d'un fichier
- **GetFileInfoAsync** : Récupère les informations d'un fichier
- **FileInfo** : DTO pour transporter les métadonnées de fichier

**Pourquoi dans Application ?** : L'interface est dans Application car elle définit un contrat métier, indépendant de l'implémentation (fichier local, cloud, etc.).

---

#### 4.1.2. Interface IAudioMetadataService
**Fichier :** `HeptaNet.Application/Services/Audio/IAudioMetadataService.cs`

**Création :** Nouveau fichier

**Description :** Interface pour extraire les métadonnées des fichiers audio.

**Code :**
```csharp
public interface IAudioMetadataService
{
    Task<int?> GetAudioDurationAsync(IFormFile audioFile);
}
```

**Explication :**
- **GetAudioDurationAsync** : Extrait la durée d'un fichier audio en secondes
- Retourne `int?` car l'extraction peut échouer (fichier corrompu, format non supporté)

**Pourquoi dans Application ?** : Même principe que IFileStorageService - abstraction métier.

---

#### 4.1.3. Implémentation LocalFileStorageService
**Fichier :** `HeptaNet.Infrastructure/FileStorage/LocalFileStorageService.cs`

**Création :** Nouveau fichier

**Description :** Implémentation du stockage de fichiers sur le système de fichiers local.

**Points Clés :**
```csharp
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    
    public LocalFileStorageService(IWebHostEnvironment environment, ILogger<LocalFileStorageService> logger)
    {
        _basePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }
    
    public async Task<string> SaveFileAsync(IFormFile file, string folder, int userId)
    {
        var userFolder = Path.Combine(_basePath, folder, userId.ToString());
        // Création du dossier utilisateur si nécessaire
        // Génération d'un nom de fichier unique avec GUID
        // Sauvegarde du fichier
        // Retour de l'URL relative : /uploads/{folder}/{userId}/{fileName}
    }
}
```

**Explication :**
- **Structure des dossiers** : `uploads/{folder}/{userId}/` pour organiser les fichiers par utilisateur
- **Nom de fichier unique** : Utilisation de `Guid.NewGuid()` pour éviter les collisions
- **URL relative** : Retourne `/uploads/...` pour être utilisé avec `UseStaticFiles()`
- **Gestion d'erreurs** : Logging des erreurs et propagation des exceptions

**Pourquoi dans Infrastructure ?** : Cette implémentation dépend de `IWebHostEnvironment` (ASP.NET Core) et du système de fichiers, donc elle appartient à la couche Infrastructure.

---

#### 4.1.4. Implémentation AudioMetadataService
**Fichier :** `HeptaNet.Infrastructure/Audio/AudioMetadataService.cs`

**Création :** Nouveau fichier

**Description :** Implémentation utilisant NAudio pour extraire la durée des fichiers audio.

**Dépendance Ajoutée :**
```xml
<PackageReference Include="NAudio" Version="2.2.1" />
```

**Code :**
```csharp
public class AudioMetadataService : IAudioMetadataService
{
    public async Task<int?> GetAudioDurationAsync(IFormFile audioFile)
    {
        var tempPath = Path.GetTempFileName();
        try
        {
            // Copier le fichier dans un fichier temporaire
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            // Utiliser NAudio pour lire la durée
            using (var audioFileReader = new AudioFileReader(tempPath))
            {
                var duration = (int)audioFileReader.TotalTime.TotalSeconds;
                File.Delete(tempPath);
                return duration;
            }
        }
        catch (Exception ex)
        {
            // Log et retourne null si l'extraction échoue
            _logger.LogWarning(ex, $"Could not extract audio duration");
            return null;
        }
    }
}
```

**Explication :**
- **Fichier temporaire** : NAudio nécessite un fichier sur disque, donc on copie d'abord le fichier uploadé
- **AudioFileReader** : Classe NAudio qui lit les métadonnées audio
- **Gestion d'erreurs** : Si l'extraction échoue (format non supporté, fichier corrompu), retourne `null` sans faire planter l'application
- **Nettoyage** : Suppression du fichier temporaire même en cas d'erreur

**Pourquoi dans Infrastructure ?** : Utilise une bibliothèque externe (NAudio) et dépend du système de fichiers.

---

### 4.2. Modification des DTOs

#### 4.2.1. Enrichissement de MessageAttachmentDto
**Fichier :** `HeptaNet.Domain/Dto/MessageDto.cs`

**Modification :** Ajout de la propriété `FullFileUrl`

**Avant :**
```csharp
public class MessageAttachmentDto
{
    public int AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? DurationSeconds { get; set; }
}
```

**Après :**
```csharp
public class MessageAttachmentDto
{
    public int AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FullFileUrl { get; set; } = string.Empty; // ✅ NOUVEAU
    public long FileSize { get; set; }
    public int? DurationSeconds { get; set; }
}
```

**Explication :**
- **FileUrl** : URL relative (`/uploads/voice-messages/1/file.wav`) - pour référence interne
- **FullFileUrl** : URL complète (`http://localhost:5106/uploads/voice-messages/1/file.wav`) - pour accès direct depuis le client
- Permet au client de télécharger/écouter directement le fichier sans construire l'URL

---

#### 4.2.2. Création de MessageWithAttachmentsDto
**Fichier :** `HeptaNet.Domain/Dto/MessageDto.cs`

**Modification :** Nouveau DTO ajouté

**Code :**
```csharp
public class MessageWithAttachmentsDto
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public int? ReplyToMessageId { get; set; }
    public MessageStatus Status { get; set; }
    public List<MessageAttachmentDto> Attachments { get; set; } = new();
    
    // Propriétés calculées pour faciliter l'accès
    public bool HasAudio => Attachments.Any(a => a.ContentType.StartsWith("audio/"));
    public MessageAttachmentDto? AudioAttachment => Attachments.FirstOrDefault(a => a.ContentType.StartsWith("audio/"));
}
```

**Explication :**
- **DTO complet** : Contient toutes les informations du message + ses attachments
- **HasAudio** : Propriété calculée pour vérifier rapidement si le message contient un audio
- **AudioAttachment** : Propriété calculée pour accéder directement à l'attachment audio (évite de filtrer côté client)
- **Utilité** : Simplifie l'utilisation côté client en fournissant toutes les données nécessaires

---

### 4.3. Modification de MessageService

#### 4.3.1. Injection des Nouveaux Services
**Fichier :** `HeptaNet.API/Services/MessageService.cs`

**Modification :** Ajout des dépendances dans le constructeur

**Avant :**
```csharp
public class MessageService : IMessageService
{
    private readonly MyAppContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<MessageService> _logger;
    
    public MessageService(
        MyAppContext context, 
        IHubContext<ChatHub> hubContext,
        ILogger<MessageService> logger)
    {
        // ...
    }
}
```

**Après :**
```csharp
public class MessageService : IMessageService
{
    private readonly MyAppContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<MessageService> _logger;
    private readonly IFileStorageService _fileStorageService;        // ✅ NOUVEAU
    private readonly IAudioMetadataService _audioMetadataService;     // ✅ NOUVEAU
    private readonly IWebHostEnvironment _environment;                // ✅ NOUVEAU
    
    public MessageService(
        MyAppContext context, 
        IHubContext<ChatHub> hubContext,
        ILogger<MessageService> logger,
        IFileStorageService fileStorageService,                      // ✅ NOUVEAU
        IAudioMetadataService audioMetadataService,                   // ✅ NOUVEAU
        IWebHostEnvironment environment)                              // ✅ NOUVEAU
    {
        // ...
    }
}
```

**Explication :**
- **IFileStorageService** : Pour sauvegarder les fichiers audio
- **IAudioMetadataService** : Pour extraire la durée des fichiers audio
- **IWebHostEnvironment** : Pour obtenir le chemin de base (utilisé dans les nouvelles méthodes)

---

#### 4.3.2. Modification de SendMessageAsync
**Fichier :** `HeptaNet.API/Services/MessageService.cs`

**Modification :** Ajout du paramètre optionnel `IFormFile? audioFile`

**Avant :**
```csharp
public async Task<Message> SendMessageAsync(SendMessageDto dto, int senderId)
{
    // Création et sauvegarde du message
}
```

**Après :**
```csharp
public async Task<Message> SendMessageAsync(SendMessageDto dto, int senderId, IFormFile? audioFile = null)
{
    // Création du message
    var message = new Message { /* ... */ };
    _context.Messages.Add(message);
    await _context.SaveChangesAsync();
    
    // ✅ NOUVEAU : Gestion des messages vocaux
    if (audioFile != null && dto.Type == MessageType.Audio)
    {
        // 1. Sauvegarder le fichier
        var fileUrl = await _fileStorageService.SaveFileAsync(
            audioFile, 
            "voice-messages", 
            senderId);
        
        // 2. Extraire la durée
        var duration = await _audioMetadataService.GetAudioDurationAsync(audioFile);
        
        // 3. Créer l'attachment
        var attachment = new MessageAttachment
        {
            MessageId = message.MessageId,
            FileName = audioFile.FileName,
            ContentType = audioFile.ContentType,
            FileUrl = fileUrl,
            FileSize = audioFile.Length,
            DurationSeconds = duration
        };
        _context.MessageAttachments.Add(attachment);
        
        // 4. Mettre à jour le contenu du message avec la durée
        if (duration.HasValue)
        {
            message.Content = $"🎤 Message vocal ({duration}s)";
        }
        else
        {
            message.Content = "🎤 Message vocal";
        }
    }
    
    // Mise à jour de la conversation et compteurs non lus
    // ...
    
    // ✅ NOUVEAU : Inclure les attachments dans le DTO SignalR
    var attachments = await _context.MessageAttachments
        .Where(a => a.MessageId == message.MessageId)
        .ToListAsync();
    
    var messageDto = new MessageReceivedDto
    {
        // ... propriétés existantes
        Attachments = attachments.Select(a => new MessageAttachmentDto
        {
            AttachmentId = a.AttachmentId,
            FileName = a.FileName,
            ContentType = a.ContentType,
            FileUrl = a.FileUrl,
            FullFileUrl = a.FileUrl, // Sera complété côté contrôleur
            FileSize = a.FileSize,
            DurationSeconds = a.DurationSeconds
        }).ToList()
    };
    
    // Broadcast SignalR avec attachments
    // ...
    
    await _context.SaveChangesAsync();
    return message;
}
```

**Explication :**
- **Paramètre optionnel** : `audioFile` est optionnel pour ne pas casser les appels existants
- **Vérification du type** : Traite uniquement si `Type == MessageType.Audio` et `audioFile != null`
- **Workflow** : Sauvegarde fichier → Extraction durée → Création attachment → Mise à jour contenu
- **Attachments dans SignalR** : Les attachments sont maintenant inclus dans le broadcast SignalR pour notification en temps réel

---

#### 4.3.3. Nouvelle Méthode GetConversationMessagesWithAttachmentsAsync
**Fichier :** `HeptaNet.API/Services/MessageService.cs`

**Création :** Nouvelle méthode dans l'interface et l'implémentation

**Code :**
```csharp
// Interface
Task<IEnumerable<MessageWithAttachmentsDto>> GetConversationMessagesWithAttachmentsAsync(
    int conversationId, 
    int userId, 
    string baseUrl,
    int page = 1, 
    int pageSize = 50);

// Implémentation
public async Task<IEnumerable<MessageWithAttachmentsDto>> GetConversationMessagesWithAttachmentsAsync(
    int conversationId, 
    int userId, 
    string baseUrl,
    int page = 1, 
    int pageSize = 50)
{
    // Récupérer les messages avec leurs relations
    var messages = await _context.Messages
        .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
        .OrderByDescending(m => m.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Include(m => m.Sender)
        .Include(m => m.Attachments)
        .Include(m => m.ReplyToMessage)
        .ToListAsync();
    
    // Mapper vers MessageWithAttachmentsDto avec URLs complètes
    return messages.Select(m => new MessageWithAttachmentsDto
    {
        MessageId = m.MessageId,
        ConversationId = m.ConversationId,
        SenderId = m.SenderId,
        SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "Unknown",
        SenderAvatar = m.Sender?.AvatarUrl,
        Type = m.Type,
        Content = m.Content,
        CreatedAt = m.CreatedAt,
        EditedAt = m.EditedAt,
        ReplyToMessageId = m.ReplyToMessageId,
        Status = m.Status,
        Attachments = m.Attachments.Select(a => new MessageAttachmentDto
        {
            AttachmentId = a.AttachmentId,
            FileName = a.FileName,
            ContentType = a.ContentType,
            FileUrl = a.FileUrl,
            FullFileUrl = $"{baseUrl.TrimEnd('/')}{a.FileUrl}", // ✅ URL complète
            FileSize = a.FileSize,
            DurationSeconds = a.DurationSeconds
        }).ToList()
    });
}
```

**Explication :**
- **Séparation des responsabilités** : Nouvelle méthode dédiée pour éviter de modifier l'existante
- **baseUrl** : Reçu en paramètre depuis le contrôleur (ex: `http://localhost:5106`)
- **Construction FullFileUrl** : Concaténation de `baseUrl` et `FileUrl` pour créer l'URL complète
- **Include** : Utilisation d'Entity Framework Include pour charger les relations en une seule requête
- **Mapping** : Transformation des entités en DTOs avec toutes les informations nécessaires

---

### 4.4. Modification de MessagesController

#### 4.4.1. Injection de IWebHostEnvironment
**Fichier :** `HeptaNet.API/Controllers/MessagesController.cs`

**Modification :** Ajout dans le constructeur

**Code :**
```csharp
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IConversationService _conversationService;
    private readonly ILogger<MessagesController> _logger;
    private readonly IWebHostEnvironment _environment; // ✅ NOUVEAU
    
    public MessagesController(
        IMessageService messageService,
        IConversationService conversationService,
        ILogger<MessagesController> logger,
        IWebHostEnvironment environment) // ✅ NOUVEAU
    {
        // ...
    }
}
```

**Explication :**
- Nécessaire pour construire le chemin complet des fichiers lors de la lecture des messages vocaux

---

#### 4.4.2. Nouvel Endpoint SendVoiceMessage
**Fichier :** `HeptaNet.API/Controllers/MessagesController.cs`

**Création :** Nouvel endpoint `POST /api/messages/voice`

**Code :**
```csharp
[HttpPost("voice")]
[RequestSizeLimit(10_000_000)] // 10 MB max
public async Task<IActionResult> SendVoiceMessage([FromForm] SendVoiceMessageRequest request)
{
    try
    {
        var userId = GetCurrentUserId();
        
        // ✅ Validation du fichier
        if (request.AudioFile == null || request.AudioFile.Length == 0)
        {
            return BadRequest(new { success = false, message = "Audio file is required" });
        }
        
        // ✅ Validation de la taille (max 10 MB)
        if (request.AudioFile.Length > 10_000_000)
        {
            return BadRequest(new { success = false, message = "File size exceeds 10 MB limit" });
        }
        
        // ✅ Validation du type de fichier
        var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac" };
        var extension = Path.GetExtension(request.AudioFile.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { 
                success = false, 
                message = "Invalid file type. Allowed: mp3, wav, ogg, m4a, aac" 
            });
        }
        
        // Créer le DTO pour MessageService
        var sendMessageDto = new SendMessageDto
        {
            ConversationId = request.ConversationId,
            ReceiverId = request.ReceiverId,
            GroupId = request.GroupId,
            Type = MessageType.Audio,
            Content = "🎤 Message vocal", // Sera mis à jour avec la durée
            ReplyToMessageId = request.ReplyToMessageId
        };
        
        // Envoyer le message avec le fichier
        var message = await _messageService.SendMessageAsync(
            sendMessageDto, 
            userId, 
            request.AudioFile);
        
        return Ok(new
        {
            success = true,
            message = "Voice message sent successfully",
            data = message
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error sending voice message");
        return StatusCode(500, new { success = false, message = "Failed to send voice message" });
    }
}
```

**Explication :**
- **Content-Type** : `multipart/form-data` (géré par `[FromForm]`)
- **RequestSizeLimit** : Limite la taille de la requête à 10 MB
- **Validations** : 
  - Fichier requis
  - Taille max 10 MB
  - Types de fichiers autorisés uniquement
- **Workflow** : Validation → Création DTO → Appel Service → Réponse

---

#### 4.4.3. Modification de GetConversationMessages
**Fichier :** `HeptaNet.API/Controllers/MessagesController.cs`

**Modification :** Utilisation de la nouvelle méthode avec attachments

**Avant :**
```csharp
[HttpGet("conversation/{conversationId}")]
public async Task<IActionResult> GetConversationMessages(...)
{
    var messages = await _messageService.GetConversationMessagesAsync(conversationId, userId, page, pageSize);
    return Ok(new { success = true, data = messages, page, pageSize });
}
```

**Après :**
```csharp
[HttpGet("conversation/{conversationId}")]
public async Task<IActionResult> GetConversationMessages(...)
{
    // ✅ Construire l'URL de base
    var baseUrl = $"{Request.Scheme}://{Request.Host}";
    
    // ✅ Utiliser la nouvelle méthode avec attachments
    var messages = await _messageService.GetConversationMessagesWithAttachmentsAsync(
        conversationId, 
        userId, 
        baseUrl,
        page, 
        pageSize);
    
    return Ok(new { success = true, data = messages, page, pageSize });
}
```

**Explication :**
- **baseUrl** : Construction dynamique depuis la requête (`http://localhost:5106`)
- **Nouvelle méthode** : Utilise `GetConversationMessagesWithAttachmentsAsync` au lieu de l'ancienne
- **Résultat** : Les messages retournés contiennent maintenant les attachments avec `FullFileUrl`

---

#### 4.4.4. Modification de GetMessage
**Fichier :** `HeptaNet.API/Controllers/MessagesController.cs`

**Modification :** Retourne maintenant un `MessageWithAttachmentsDto` avec URLs complètes

**Avant :**
```csharp
[HttpGet("{messageId}")]
public async Task<IActionResult> GetMessage(int messageId)
{
    var message = await _messageService.GetMessageByIdAsync(messageId);
    return Ok(new { success = true, data = message });
}
```

**Après :**
```csharp
[HttpGet("{messageId}")]
public async Task<IActionResult> GetMessage(int messageId)
{
    var userId = GetCurrentUserId();
    var message = await _messageService.GetMessageByIdAsync(messageId);
    
    if (message == null)
    {
        return NotFound(new { success = false, message = "Message not found" });
    }
    
    // ✅ Vérifier les permissions
    var conversation = await _conversationService.GetConversationByIdAsync(message.ConversationId, userId);
    if (conversation == null)
    {
        return Forbid();
    }
    
    // ✅ Construire l'URL de base
    var baseUrl = $"{Request.Scheme}://{Request.Host}";
    
    // ✅ Construire le DTO avec attachments et URLs complètes
    var messageDto = new MessageWithAttachmentsDto
    {
        MessageId = message.MessageId,
        // ... toutes les propriétés
        Attachments = message.Attachments.Select(a => new MessageAttachmentDto
        {
            // ...
            FullFileUrl = $"{baseUrl.TrimEnd('/')}{a.FileUrl}", // ✅ URL complète
        }).ToList()
    };
    
    return Ok(new { success = true, data = messageDto });
}
```

**Explication :**
- **Vérification permissions** : S'assure que l'utilisateur a accès à la conversation
- **Mapping manuel** : Construction du DTO avec toutes les propriétés nécessaires
- **FullFileUrl** : Construction de l'URL complète pour chaque attachment

---

#### 4.4.5. Nouvel Endpoint GetVoiceMessage
**Fichier :** `HeptaNet.API/Controllers/MessagesController.cs`

**Création :** Nouvel endpoint `GET /api/messages/{messageId}/voice`

**Code :**
```csharp
[HttpGet("{messageId}/voice")]
public async Task<IActionResult> GetVoiceMessage(int messageId)
{
    try
    {
        var userId = GetCurrentUserId();
        
        // ✅ Récupérer le message avec ses attachments
        var message = await _messageService.GetMessageByIdAsync(messageId);
        
        if (message == null)
        {
            return NotFound(new { success = false, message = "Message not found" });
        }
        
        // ✅ Vérifier que l'utilisateur a accès à cette conversation
        var conversation = await _conversationService.GetConversationByIdAsync(
            message.ConversationId, 
            userId);
        
        if (conversation == null)
        {
            return Forbid();
        }
        
        // ✅ Vérifier que c'est un message vocal
        if (message.Type != MessageType.Audio)
        {
            return BadRequest(new { success = false, message = "This message is not a voice message" });
        }
        
        // ✅ Récupérer l'attachment audio
        var audioAttachment = message.Attachments
            .FirstOrDefault(a => a.ContentType.StartsWith("audio/"));
        
        if (audioAttachment == null)
        {
            return NotFound(new { success = false, message = "Audio file not found for this message" });
        }
        
        // ✅ Construire le chemin complet du fichier
        var filePath = audioAttachment.FileUrl
            .Replace("/uploads/", "", StringComparison.OrdinalIgnoreCase)
            .Replace('/', Path.DirectorySeparatorChar);
        
        var webRootPath = Path.Combine(
            _environment.WebRootPath ?? _environment.ContentRootPath, 
            "uploads", 
            filePath);
        
        // ✅ Vérifier que le fichier existe
        if (!System.IO.File.Exists(webRootPath))
        {
            return NotFound(new { success = false, message = "Audio file not found on server" });
        }
        
        // ✅ Lire le fichier
        var fileBytes = await System.IO.File.ReadAllBytesAsync(webRootPath);
        var contentType = audioAttachment.ContentType;
        
        // ✅ Retourner le fichier
        return File(fileBytes, contentType, audioAttachment.FileName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving voice message");
        return StatusCode(500, new { success = false, message = "Failed to retrieve voice message" });
    }
}
```

**Explication :**
- **Vérifications multiples** : Message existe, permissions, type Audio, attachment existe, fichier existe sur disque
- **Construction du chemin** : Conversion de l'URL relative en chemin système
- **Retour File** : Utilise `File()` pour retourner le fichier binaire avec le bon Content-Type
- **Gestion d'erreurs** : Retourne des erreurs JSON structurées à chaque étape

---

### 4.5. Création du Middleware de Gestion d'Erreurs

#### 4.5.1. GlobalExceptionHandlerMiddleware
**Fichier :** `HeptaNet.API/Middleware/GlobalExceptionHandlerMiddleware.cs`

**Création :** Nouveau fichier

**Description :** Middleware qui capture toutes les exceptions non gérées et retourne des réponses JSON structurées.

**Code :**
```csharp
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        // ✅ Déterminer le code HTTP selon le type d'exception
        var (statusCode, message) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "Un paramètre requis est manquant"),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Accès non autorisé"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Ressource non trouvée"),
            FileNotFoundException => (HttpStatusCode.NotFound, "Fichier non trouvé"),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, 
                _environment.IsDevelopment() 
                    ? exception.Message 
                    : "Une erreur interne s'est produite. Veuillez réessayer plus tard.")
        };

        context.Response.StatusCode = (int)statusCode;

        // ✅ Construire la réponse JSON
        var response = new
        {
            success = false,
            message = message,
            error = _environment.IsDevelopment() ? new
            {
                type = exception.GetType().Name,
                message = exception.Message,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException != null ? new
                {
                    type = exception.InnerException.GetType().Name,
                    message = exception.InnerException.Message
                } : null
            } : null
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
```

**Explication :**
- **Capture globale** : Intercepte toutes les exceptions non gérées dans le pipeline
- **Mapping des exceptions** : Utilise un switch expression pour mapper les types d'exceptions aux codes HTTP appropriés
- **Environnement** : 
  - **Development** : Retourne les détails complets (type, message, stack trace)
  - **Production** : Retourne un message générique pour la sécurité
- **Format JSON** : Toujours du JSON, jamais de page HTML
- **CamelCase** : Utilise `JsonNamingPolicy.CamelCase` pour la cohérence avec les autres réponses

---

### 4.6. Modification de Program.cs

#### 4.6.1. Enregistrement des Nouveaux Services
**Fichier :** `HeptaNet.API/Program.cs`

**Modification :** Ajout de l'enregistrement DI

**Code :**
```csharp
// ✅ NOUVEAU : Register file storage and audio services
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IAudioMetadataService, AudioMetadataService>();
```

**Explication :**
- **Scoped** : Une instance par requête HTTP (approprié pour les services qui utilisent le contexte)
- **Injection de dépendances** : Permet l'utilisation dans les contrôleurs et services

---

#### 4.6.2. Activation des Fichiers Statiques
**Fichier :** `HeptaNet.API/Program.cs`

**Modification :** Ajout de `app.UseStaticFiles()`

**Code :**
```csharp
// Enable static files for serving uploaded files
app.UseStaticFiles();
```

**Explication :**
- **Serving files** : Permet de servir les fichiers depuis le dossier `wwwroot` et `uploads`
- **URLs** : Les fichiers dans `/uploads/...` sont accessibles directement via HTTP
- **Ordre important** : Doit être avant `UseRouting()` et `MapControllers()`

---

#### 4.6.3. Enregistrement du Middleware
**Fichier :** `HeptaNet.API/Program.cs`

**Modification :** Ajout du middleware dans le pipeline

**Code :**
```csharp
using HeptaNet.API.Middleware; // ✅ NOUVEAU

// ...

// Global exception handler middleware (doit être après UseRouting mais avant MapControllers)
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
```

**Explication :**
- **Ordre du pipeline** : 
  1. `UseSwagger()` / `UseSwaggerUI()`
  2. `UseHttpsRedirection()` (si production)
  3. `UseStaticFiles()`
  4. `UseCors()`
  5. `UseAuthentication()`
  6. `UseAuthorization()`
  7. `UseMiddleware<GlobalExceptionHandlerMiddleware>()` ← **ICI**
  8. `MapControllers()`
- **Position** : Après l'authentification/autorisation pour capturer les erreurs de ces middlewares aussi

---

### 4.7. Création de SendVoiceMessageRequest

#### 4.7.1. Request pour Messages Vocaux
**Fichier :** `HeptaNet.API/Requests/SendVoiceMessageRequest.cs`

**Création :** Nouveau fichier

**Code :**
```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HeptaNet.API.Requests;

public class SendVoiceMessageRequest
{
    [Required(ErrorMessage = "L'ID de la conversation est requis")]
    public int ConversationId { get; set; }
    
    public int? ReceiverId { get; set; }
    public int? GroupId { get; set; }
    public int? ReplyToMessageId { get; set; }
    
    [Required(ErrorMessage = "Le fichier audio est requis")]
    public IFormFile AudioFile { get; set; } = null!;
}
```

**Explication :**
- **Pourquoi dans API/Requests ?** : Utilise `IFormFile` qui est spécifique à ASP.NET Core, donc ne peut pas être dans Domain
- **Validation** : Utilise les Data Annotations pour la validation automatique
- **IFormFile** : Type ASP.NET Core pour les fichiers uploadés via multipart/form-data

---

### 4.8. Modification de MessageRequest.cs

#### 4.8.1. Déplacement des Requests
**Fichier :** `HeptaNet.Domain/Request/MessageRequest.cs`

**Modification :** Ajout de requests déplacées depuis les contrôleurs

**Code :**
```csharp
using System.ComponentModel.DataAnnotations;

namespace HeptaNet.Domain.Request;

public record EditMessageRequest
{
    [Required(ErrorMessage = "Le contenu du message est requis")]
    public string Content { get; init; } = string.Empty;
}

public record CreatePrivateConversationRequest
{
    [Required(ErrorMessage = "L'ID de l'autre utilisateur est requis")]
    public int OtherUserId { get; init; }
}

public record CreateGroupConversationRequest
{
    [Required(ErrorMessage = "L'ID du groupe est requis")]
    public int GroupId { get; init; }
}
```

**Explication :**
- **Organisation** : Centralisation des requests dans Domain/Request
- **Records** : Utilisation de `record` pour l'immutabilité
- **Validation** : Data Annotations pour validation automatique

---

### 4.9. Ajout de la Dépendance NAudio

#### 4.9.1. Package NuGet
**Fichier :** `HeptaNet.Infrastructure/HeptaNet.Infrastructure.csproj`

**Modification :** Ajout du package

**Code :**
```xml
<ItemGroup>
  <PackageReference Include="NAudio" Version="2.2.1" />
</ItemGroup>
```

**Explication :**
- **NAudio** : Bibliothèque .NET pour travailler avec les fichiers audio
- **Version 2.2.1** : Version stable et compatible
- **Utilisation** : Utilisée dans `AudioMetadataService` pour extraire la durée

---

## 5. Flux de Données

### 5.1. Envoi d'un Message Vocal

```
┌─────────────┐
│   Client    │
│  (Swagger/  │
│   Flutter)  │
└──────┬──────┘
       │
       │ POST /api/messages/voice
       │ multipart/form-data
       │ { conversationId, audioFile }
       ▼
┌─────────────────────────┐
│ MessagesController      │
│ SendVoiceMessage()      │
└──────┬──────────────────┘
       │
       │ 1. Validation (taille, type)
       │ 2. GetCurrentUserId()
       │ 3. Création SendMessageDto
       ▼
┌─────────────────────────┐
│ MessageService          │
│ SendMessageAsync()      │
└──────┬──────────────────┘
       │
       │ 4. Création Message (Type: Audio)
       │ 5. Sauvegarde en DB
       │
       ├───────────────────┐
       │                   │
       ▼                   ▼
┌──────────────────┐  ┌──────────────────┐
│ IFileStorage     │  │ IAudioMetadata   │
│ Service          │  │ Service          │
│                  │  │                  │
│ SaveFileAsync()  │  │ GetAudioDuration │
│                  │  │ Async()          │
└────────┬─────────┘  └────────┬─────────┘
         │                     │
         │ 6. Sauvegarde       │ 7. Extraction
         │    fichier          │    durée
         │                     │
         └──────────┬──────────┘
                    │
                    ▼
         ┌──────────────────────┐
         │ Création              │
         │ MessageAttachment     │
         │ (FileUrl, Duration)   │
         └──────────┬────────────┘
                    │
                    ▼
         ┌──────────────────────┐
         │ Mise à jour          │
         │ Message.Content      │
         │ "🎤 Message (5s)"    │
         └──────────┬────────────┘
                    │
                    ▼
         ┌──────────────────────┐
         │ Broadcast SignalR    │
         │ avec Attachments     │
         └──────────┬────────────┘
                    │
                    ▼
         ┌──────────────────────┐
         │ Réponse JSON         │
         │ { success, data }    │
         └──────────────────────┘
```

### 5.2. Récupération des Messages avec Attachments

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │
       │ GET /api/messages/conversation/{id}
       │ ?page=1&pageSize=50
       ▼
┌─────────────────────────┐
│ MessagesController      │
│ GetConversationMessages │
└──────┬──────────────────┘
       │
       │ 1. Vérification permissions
       │ 2. Construction baseUrl
       │    "http://localhost:5106"
       ▼
┌─────────────────────────┐
│ MessageService          │
│ GetConversationMessages │
│ WithAttachmentsAsync()  │
└──────┬──────────────────┘
       │
       │ 3. Requête EF Core
       │    .Include(m => m.Attachments)
       │    .Include(m => m.Sender)
       │
       ▼
┌─────────────────────────┐
│ Mapping vers            │
│ MessageWithAttachmentsDto│
│                         │
│ FullFileUrl =           │
│   baseUrl + FileUrl     │
└──────────┬──────────────┘
           │
           ▼
┌─────────────────────────┐
│ Réponse JSON            │
│ {                       │
│   success: true,        │
│   data: [               │
│     {                   │
│       messageId,        │
│       attachments: [   │
│         {               │
│           fullFileUrl:  │
│             "http://..."│
│         }               │
│       ]                 │
│     }                   │
│   ]                     │
│ }                       │
└─────────────────────────┘
```

### 5.3. Lecture d'un Message Vocal

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │
       │ GET /api/messages/{id}/voice
       ▼
┌─────────────────────────┐
│ MessagesController      │
│ GetVoiceMessage()       │
└──────┬──────────────────┘
       │
       │ 1. Récupération message
       │ 2. Vérification permissions
       │ 3. Vérification Type == Audio
       │ 4. Récupération attachment
       │
       ▼
┌─────────────────────────┐
│ Construction chemin      │
│ FileUrl → chemin système│
│ /uploads/... →          │
│ C:\...\uploads\...     │
└──────┬──────────────────┘
       │
       │ 5. Vérification existence
       │ 6. Lecture fichier
       │
       ▼
┌─────────────────────────┐
│ File(fileBytes,         │
│      contentType,       │
│      fileName)          │
└──────┬──────────────────┘
       │
       ▼
┌─────────────────────────┐
│ Réponse binaire         │
│ Content-Type: audio/wav │
│ Body: [fichier audio]   │
└─────────────────────────┘
```

---

## 6. Endpoints API

### 6.1. POST /api/messages/voice

**Description :** Envoie un message vocal

**Authentification :** Requise (JWT)

**Content-Type :** `multipart/form-data`

**Paramètres :**
- `conversationId` (int, requis)
- `receiverId` (int?, optionnel)
- `groupId` (int?, optionnel)
- `replyToMessageId` (int?, optionnel)
- `audioFile` (IFormFile, requis, max 10 MB)

**Validations :**
- Fichier requis
- Taille max : 10 MB
- Types autorisés : `.mp3`, `.wav`, `.ogg`, `.m4a`, `.aac`

**Réponse Succès (200) :**
```json
{
  "success": true,
  "message": "Voice message sent successfully",
  "data": {
    "messageId": 123,
    "conversationId": 1,
    "type": "Audio",
    "content": "🎤 Message vocal (5s)",
    "createdAt": "2024-01-01T10:00:00Z"
  }
}
```

**Réponse Erreur (400) :**
```json
{
  "success": false,
  "message": "File size exceeds 10 MB limit"
}
```

---

### 6.2. GET /api/messages/conversation/{conversationId}

**Description :** Récupère les messages d'une conversation avec attachments et URLs complètes

**Authentification :** Requise (JWT)

**Query Parameters :**
- `page` (int, default: 1)
- `pageSize` (int, default: 50)

**Réponse Succès (200) :**
```json
{
  "success": true,
  "data": [
    {
      "messageId": 123,
      "conversationId": 1,
      "senderId": 1,
      "senderName": "John Doe",
      "type": "Audio",
      "content": "🎤 Message vocal (5s)",
      "createdAt": "2024-01-01T10:00:00Z",
      "attachments": [
        {
          "attachmentId": 1,
          "fileName": "voice_12345.wav",
          "contentType": "audio/wav",
          "fileUrl": "/uploads/voice-messages/1/voice_12345.wav",
          "fullFileUrl": "http://localhost:5106/uploads/voice-messages/1/voice_12345.wav",
          "fileSize": 102400,
          "durationSeconds": 5
        }
      ],
      "hasAudio": true,
      "audioAttachment": {
        "attachmentId": 1,
        "fullFileUrl": "http://localhost:5106/uploads/voice-messages/1/voice_12345.wav",
        "durationSeconds": 5
      }
    }
  ],
  "page": 1,
  "pageSize": 50
}
```

---

### 6.3. GET /api/messages/{messageId}

**Description :** Récupère un message spécifique avec attachments et URLs complètes

**Authentification :** Requise (JWT)

**Réponse :** Même structure que `GetConversationMessages`, mais un seul message

---

### 6.4. GET /api/messages/{messageId}/voice

**Description :** Télécharge le fichier audio d'un message vocal

**Authentification :** Requise (JWT)

**Réponse Succès (200) :**
- Content-Type : `audio/wav`, `audio/mp3`, etc.
- Body : Fichier audio binaire

**Réponse Erreur (404) :**
```json
{
  "success": false,
  "message": "Audio file not found for this message"
}
```

---

## 7. Gestion des Erreurs

### 7.1. Middleware Global

Le `GlobalExceptionHandlerMiddleware` capture toutes les exceptions non gérées et retourne des réponses JSON structurées.

**Mapping des Exceptions :**

| Exception | Code HTTP | Message |
|-----------|----------|---------|
| `ArgumentNullException` | 400 | "Un paramètre requis est manquant" |
| `ArgumentException` | 400 | Message de l'exception |
| `UnauthorizedAccessException` | 403 | "Accès non autorisé" |
| `KeyNotFoundException` | 404 | "Ressource non trouvée" |
| `FileNotFoundException` | 404 | "Fichier non trouvé" |
| `InvalidOperationException` | 400 | Message de l'exception |
| Autres | 500 | Message selon environnement |

**Réponse en Développement :**
```json
{
  "success": false,
  "message": "Message d'erreur détaillé",
  "error": {
    "type": "ArgumentException",
    "message": "Détails de l'erreur",
    "stackTrace": "...",
    "innerException": {
      "type": "...",
      "message": "..."
    }
  }
}
```

**Réponse en Production :**
```json
{
  "success": false,
  "message": "Une erreur interne s'est produite. Veuillez réessayer plus tard.",
  "error": null
}
```

### 7.2. Gestion dans les Contrôleurs

Tous les endpoints utilisent `try-catch` pour :
- Logger les erreurs
- Retourner des réponses JSON cohérentes
- Laisser le middleware gérer les exceptions non capturées

---

## 8. Tests et Validation

### 8.1. Tests avec Swagger

#### Étape 1 : Authentification
1. Accéder à `http://localhost:5106/swagger`
2. Utiliser `/api/auth/login` pour obtenir un JWT
3. Cliquer sur "Authorize" et entrer : `Bearer [token]`

#### Étape 2 : Envoyer un Message Vocal
1. Ouvrir `POST /api/messages/voice`
2. Cliquer sur "Try it out"
3. Remplir :
   - `conversationId` : ID d'une conversation existante
   - `audioFile` : Sélectionner un fichier audio (< 10 MB)
4. Cliquer sur "Execute"
5. Vérifier : Code 200 avec `success: true`

#### Étape 3 : Récupérer les Messages
1. Ouvrir `GET /api/messages/conversation/{conversationId}`
2. Entrer l'ID de la conversation
3. Vérifier : Les messages contiennent `attachments` avec `fullFileUrl`

#### Étape 4 : Lire un Message Vocal
1. Ouvrir `GET /api/messages/{messageId}/voice`
2. Entrer l'ID d'un message vocal
3. Vérifier : Le fichier audio est téléchargé

### 8.2. Tests de Validation

#### Test 1 : Fichier Trop Volumineux
- **Action** : Envoyer un fichier > 10 MB
- **Résultat Attendu** : Code 400 avec message JSON

#### Test 2 : Type de Fichier Invalide
- **Action** : Envoyer un fichier `.txt`
- **Résultat Attendu** : Code 400 avec message JSON

#### Test 3 : Conversation Inexistante
- **Action** : Utiliser un `conversationId` invalide
- **Résultat Attendu** : Code 404 avec message JSON

#### Test 4 : Message Vocal Inexistant
- **Action** : Utiliser un `messageId` sans fichier audio
- **Résultat Attendu** : Code 404 avec message JSON

---

## 9. Résumé des Modifications

### Fichiers Créés
1. ✅ `HeptaNet.Application/Services/FileStorage/IFileStorageService.cs`
2. ✅ `HeptaNet.Application/Services/Audio/IAudioMetadataService.cs`
3. ✅ `HeptaNet.Infrastructure/FileStorage/LocalFileStorageService.cs`
4. ✅ `HeptaNet.Infrastructure/Audio/AudioMetadataService.cs`
5. ✅ `HeptaNet.API/Middleware/GlobalExceptionHandlerMiddleware.cs`
6. ✅ `HeptaNet.API/Requests/SendVoiceMessageRequest.cs`

### Fichiers Modifiés
1. ✅ `HeptaNet.Domain/Dto/MessageDto.cs` - Ajout de `FullFileUrl` et `MessageWithAttachmentsDto`
2. ✅ `HeptaNet.API/Services/MessageService.cs` - Ajout support messages vocaux et nouvelle méthode
3. ✅ `HeptaNet.API/Controllers/MessagesController.cs` - Nouveaux endpoints et modifications
4. ✅ `HeptaNet.API/Program.cs` - Enregistrement services et middleware
5. ✅ `HeptaNet.Domain/Request/MessageRequest.cs` - Ajout de requests
6. ✅ `HeptaNet.Infrastructure/HeptaNet.Infrastructure.csproj` - Ajout package NAudio

### Fonctionnalités Ajoutées
1. ✅ Envoi de messages vocaux avec validation
2. ✅ Extraction automatique de la durée audio
3. ✅ Stockage des fichiers audio
4. ✅ Récupération des messages avec URLs complètes
5. ✅ Endpoint pour lire les messages vocaux
6. ✅ Gestion d'erreurs centralisée avec JSON
7. ✅ Broadcast SignalR avec attachments

---

## 10. Points d'Attention

### 10.1. Stockage des Fichiers
- **Emplacement** : `uploads/voice-messages/{userId}/`
- **Nom de fichier** : Généré avec GUID pour éviter les collisions
- **Accès** : Via `UseStaticFiles()` pour servir les fichiers directement

### 10.2. Métadonnées Audio
- **Extraction** : Utilise NAudio, peut échouer pour certains formats
- **Gestion d'erreurs** : Retourne `null` si l'extraction échoue, ne bloque pas l'envoi
- **Affichage** : Durée affichée dans `Message.Content` si disponible

### 10.3. URLs Complètes
- **Construction** : `baseUrl` construit depuis `Request.Scheme` et `Request.Host`
- **Utilisation** : `FullFileUrl` pour accès direct depuis le client
- **Sécurité** : Vérification des permissions avant de servir les fichiers

### 10.4. Gestion d'Erreurs
- **Middleware** : Capture toutes les exceptions non gérées
- **Format** : Toujours JSON, jamais HTML
- **Environnement** : Détails complets en développement, messages génériques en production

---

## 11. Conclusion

Cette implémentation respecte les principes de Clean Architecture, ajoute la fonctionnalité de messages vocaux de manière robuste, et améliore significativement la gestion des erreurs pour une meilleure expérience développeur et utilisateur.

Toutes les modifications sont documentées, testées, et prêtes pour la production.

---

**Date de création :** 2024  
**Version :** 1.0  
**Auteur :** Équipe HeptaNet

