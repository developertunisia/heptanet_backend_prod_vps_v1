# SignalR Real-Time Messaging Guide

This guide explains how to use the SignalR real-time messaging system in HeptaNet.

## Overview

SignalR has been integrated to provide real-time messaging capabilities including:
- ✅ Private (1-on-1) messaging
- ✅ Group messaging
- ✅ Typing indicators
- ✅ Read receipts
- ✅ Message delivery status
- ✅ Online/offline status
- ✅ Message editing and deletion notifications

## Architecture

### Key Components

1. **ChatHub** (`HeptaNet.API/Hubs/ChatHub.cs`)
   - SignalR hub for real-time communication
   - Handles connection management, message broadcasting, and user presence

2. **MessageService** (`HeptaNet.Application/Services/MessageService.cs`)
   - Business logic for sending, editing, deleting messages
   - Integrates with SignalR to broadcast events

3. **ConversationService** (`HeptaNet.Application/Services/ConversationService.cs`)
   - Manages conversations (private and group)
   - Handles conversation participants and settings

4. **ConnectionTrackingService** (`HeptaNet.Application/Services/ConnectionTrackingService.cs`)
   - Tracks online users and their connections
   - Singleton service shared across all requests

## API Endpoints

### Conversations

#### Get All Conversations
```http
GET /api/conversations?includeArchived=false
Authorization: Bearer {token}
```

#### Get Specific Conversation
```http
GET /api/conversations/{conversationId}
Authorization: Bearer {token}
```

#### Create Private Conversation
```http
POST /api/conversations/private
Authorization: Bearer {token}
Content-Type: application/json

{
  "otherUserId": 123
}
```

#### Create Group Conversation
```http
POST /api/conversations/group
Authorization: Bearer {token}
Content-Type: application/json

{
  "groupId": 456
}
```

#### Archive/Unarchive Conversation
```http
POST /api/conversations/{conversationId}/archive
POST /api/conversations/{conversationId}/unarchive
Authorization: Bearer {token}
```

#### Pin/Unpin Conversation
```http
POST /api/conversations/{conversationId}/pin
POST /api/conversations/{conversationId}/unpin
Authorization: Bearer {token}
```

#### Mute/Unmute Conversation
```http
POST /api/conversations/{conversationId}/mute
POST /api/conversations/{conversationId}/unmute
Authorization: Bearer {token}
```

### Messages

#### Send Message
```http
POST /api/messages
Authorization: Bearer {token}
Content-Type: application/json

{
  "conversationId": 1,
  "receiverId": 123,        // For private messages
  "groupId": null,          // For group messages
  "type": 0,                // 0=Text, 1=Image, 2=Video, etc.
  "content": "Hello!",
  "replyToMessageId": null  // Optional: for replying to a message
}
```

#### Get Conversation Messages
```http
GET /api/messages/conversation/{conversationId}?page=1&pageSize=50
Authorization: Bearer {token}
```

#### Mark Message as Read
```http
POST /api/messages/{messageId}/read
Authorization: Bearer {token}
```

#### Mark All Messages in Conversation as Read
```http
POST /api/messages/conversation/{conversationId}/read-all
Authorization: Bearer {token}
```

#### Edit Message
```http
PUT /api/messages/{messageId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "content": "Updated message content"
}
```

#### Delete Message
```http
DELETE /api/messages/{messageId}
Authorization: Bearer {token}
```

## SignalR Hub Connection

### Hub Endpoint
```
ws://localhost:5106/hubs/chat?access_token={jwt_token}
```

### Client Connection Examples

#### JavaScript/TypeScript (Web)
```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5106/hubs/chat", {
        accessTokenFactory: () => localStorage.getItem("token")
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Start connection
await connection.start();
console.log("Connected to SignalR");
```

#### Flutter/Dart
```dart
import 'package:signalr_netcore/signalr_netcore.dart';

final hubConnection = HubConnectionBuilder()
    .withUrl(
      "http://localhost:5106/hubs/chat",
      options: HttpConnectionOptions(
        accessTokenFactory: () async => await getToken(),
      ),
    )
    .withAutomaticReconnect()
    .build();

await hubConnection.start();
```

## SignalR Events

### Client → Server (Methods to Call)

#### Join Group
```javascript
await connection.invoke("JoinGroup", groupId);
```

#### Leave Group
```javascript
await connection.invoke("LeaveGroup", groupId);
```

#### Join Conversation
```javascript
await connection.invoke("JoinConversation", conversationId);
```

#### Leave Conversation
```javascript
await connection.invoke("LeaveConversation", conversationId);
```

#### Send Private Message (via SignalR directly)
```javascript
await connection.invoke("SendPrivateMessage", receiverId, message, conversationId);
```

#### Send Group Message (via SignalR directly)
```javascript
await connection.invoke("SendGroupMessage", groupId, message, conversationId);
```

#### Typing Indicator
```javascript
await connection.invoke("UserTyping", conversationId, isGroup, groupId);
await connection.invoke("UserStoppedTyping", conversationId, isGroup, groupId);
```

#### Mark as Read
```javascript
await connection.invoke("MarkAsRead", messageId, conversationId, senderId);
```

#### Message Delivered
```javascript
await connection.invoke("MessageDelivered", messageId, senderId);
```

### Server → Client (Events to Listen)

#### User Online/Offline
```javascript
connection.on("UserOnline", (userId) => {
    console.log(`User ${userId} is now online`);
});

connection.on("UserOffline", (userId) => {
    console.log(`User ${userId} is now offline`);
});
```

#### Receive Messages
```javascript
connection.on("ReceivePrivateMessage", (message) => {
    console.log("New private message:", message);
    // message structure:
    // {
    //   conversationId, senderId, senderName, senderAvatar,
    //   type, content, createdAt, replyToMessageId, status
    // }
});

connection.on("ReceiveGroupMessage", (message) => {
    console.log("New group message:", message);
});

connection.on("NewMessage", (message) => {
    console.log("New message in conversation:", message);
});
```

#### Typing Indicators
```javascript
connection.on("UserTyping", (conversationId, userId) => {
    console.log(`User ${userId} is typing in conversation ${conversationId}`);
});

connection.on("UserStoppedTyping", (conversationId, userId) => {
    console.log(`User ${userId} stopped typing`);
});
```

#### Message Status Updates
```javascript
connection.on("MessageRead", (receipt) => {
    console.log("Message read:", receipt);
    // receipt: { messageId, conversationId, readBy, readAt }
});

connection.on("MessageDelivered", (delivery) => {
    console.log("Message delivered:", delivery);
    // delivery: { messageId, deliveredTo, deliveredAt }
});
```

#### Message Edited/Deleted
```javascript
connection.on("MessageEdited", (data) => {
    console.log("Message edited:", data);
    // data: { messageId, conversationId, newContent, editedAt }
});

connection.on("MessageDeleted", (data) => {
    console.log("Message deleted:", data);
    // data: { messageId, conversationId, deletedAt }
});
```

## Message Types

```csharp
public enum MessageType
{
    Text = 0,
    Image = 1,
    Video = 2,
    Audio = 3,
    Document = 4,
    Location = 5,
    Contact = 6,
    Sticker = 7,
    System = 8  // "User joined", "User left", etc.
}
```

## Message Status

```csharp
public enum MessageStatus
{
    Sending = 0,
    Sent = 1,
    Delivered = 2,
    Read = 3,
    Failed = 4
}
```

## Best Practices

### 1. Connection Management
- Always implement reconnection logic
- Store connection state in your application
- Handle connection errors gracefully

### 2. Sending Messages
- Use REST API for sending messages (automatically broadcasts via SignalR)
- This ensures messages are persisted in database
- SignalR methods are for real-time notifications only

### 3. Joining Groups
- Join conversation groups when opening a chat
- Join group conversations for group chats
- Leave groups when closing a chat to reduce server load

### 4. Typing Indicators
- Implement debouncing (send after 2-3 seconds of inactivity)
- Clear typing indicator on message send
- Set timeout to auto-clear typing indicator

### 5. Read Receipts
- Mark messages as read when user views them
- Batch read receipts for better performance
- Use "mark conversation as read" for efficiency

## Example: Complete Flutter Integration

```dart
class ChatService {
  HubConnection? _hubConnection;
  
  Future<void> connect() async {
    _hubConnection = HubConnectionBuilder()
        .withUrl(
          "http://localhost:5106/hubs/chat",
          options: HttpConnectionOptions(
            accessTokenFactory: () async => await getAuthToken(),
          ),
        )
        .withAutomaticReconnect()
        .build();
    
    // Listen to events
    _hubConnection!.on("ReceivePrivateMessage", _onPrivateMessage);
    _hubConnection!.on("ReceiveGroupMessage", _onGroupMessage);
    _hubConnection!.on("UserTyping", _onUserTyping);
    _hubConnection!.on("MessageRead", _onMessageRead);
    
    await _hubConnection!.start();
  }
  
  void _onPrivateMessage(List<Object?>? arguments) {
    if (arguments != null && arguments.isNotEmpty) {
      final message = arguments[0] as Map<String, dynamic>;
      // Handle new message
      print("New message: ${message['content']}");
    }
  }
  
  Future<void> sendMessage({
    required int conversationId,
    required String content,
    int? receiverId,
    int? groupId,
  }) async {
    // Use REST API to send message
    final response = await http.post(
      Uri.parse('http://localhost:5106/api/messages'),
      headers: {
        'Authorization': 'Bearer ${await getAuthToken()}',
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'conversationId': conversationId,
        'content': content,
        'receiverId': receiverId,
        'groupId': groupId,
        'type': 0, // Text
      }),
    );
    
    // Message will be automatically broadcast via SignalR
  }
  
  Future<void> joinConversation(int conversationId) async {
    await _hubConnection?.invoke("JoinConversation", args: [conversationId]);
  }
  
  Future<void> disconnect() async {
    await _hubConnection?.stop();
  }
}
```

## Troubleshooting

### Connection Issues
- Verify JWT token is valid and not expired
- Check CORS configuration if connecting from web
- Ensure SignalR endpoint is accessible

### Messages Not Received
- Verify user has joined the conversation/group
- Check if user is a participant in the conversation
- Ensure SignalR connection is established

### Authentication Failures
- Token must be passed in query string for SignalR
- Token format: `?access_token={token}` (without "Bearer" prefix)
- Ensure token contains valid claims (NameIdentifier)

## Database Migration

Before using the messaging system, run the migration:

```bash
dotnet ef migrations add AddMessagingEntities --project HeptaNet.Infrastructure --startup-project HeptaNet.API
dotnet ef database update --project HeptaNet.Infrastructure --startup-project HeptaNet.API
```

## Performance Considerations

- Messages are paginated (default 50 per page)
- Conversations have denormalized last message for quick retrieval
- Unread counts are cached in ConversationParticipant
- Connection tracking uses in-memory concurrent dictionary
- All critical queries have optimized indexes

## Security

- All endpoints require authentication
- Users can only access conversations they're participants in
- Users can only edit/delete their own messages
- SignalR uses JWT for authentication
- All database operations validate user permissions

