using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HeptaNet.Domain.Models;

namespace HeptaNet.Infrastructure.Database;

public class MyAppContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public MyAppContext(DbContextOptions<MyAppContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<EmailVerification> EmailVerifications { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<BroadcastMessage> BroadcastMessages { get; set; }
    public DbSet<BlacklistLog> BlacklistLogs { get; set; }
    public DbSet<AuthorizedEmail> AuthorizedEmails { get; set; }
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    
    // New DbSets for core messaging features
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    public DbSet<MessageAttachment> MessageAttachments { get; set; }
    public DbSet<MessageReceipt> MessageReceipts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========== USER CONFIGURATION ==========
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.WhatsAppNumber).HasMaxLength(20);
            entity.Property(e => e.IsBlacklisted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasMany(e => e.GroupMembers)
                .WithOne(gm => gm.User)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.SentMessages)
                .WithOne(m => m.Sender)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.ReceivedMessages)
                .WithOne(m => m.Receiver)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.BroadcastMessages)
                .WithOne(bm => bm.Sender)
                .HasForeignKey(bm => bm.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CreatedGroups)
                .WithOne(g => g.Creator)
                .HasForeignKey(g => g.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Indexes for performance
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.WhatsAppNumber);
            entity.HasIndex(e => e.IsBlacklisted);
        });

        // ========== CONVERSATION CONFIGURATION ==========
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            // Relationship with Users (for private chats)
            entity.HasOne(e => e.User1)
                .WithMany()
                .HasForeignKey(e => e.User1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User2)
                .WithMany()
                .HasForeignKey(e => e.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship with Group
            entity.HasOne(e => e.Group)
                .WithOne(g => g.Conversation)
                .HasForeignKey<Conversation>(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with LastMessage
            entity.HasOne(e => e.LastMessage)
                .WithMany()
                .HasForeignKey(e => e.LastMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            // CRITICAL INDEXES for conversation queries
            entity.HasIndex(e => new { e.User1Id, e.User2Id, e.Type, e.IsDeleted })
                .HasDatabaseName("IX_Conversation_PrivateChat");
            
            entity.HasIndex(e => new { e.GroupId, e.IsDeleted })
                .HasDatabaseName("IX_Conversation_Group");
            
            entity.HasIndex(e => new { e.LastMessageAt, e.IsDeleted })
                .HasDatabaseName("IX_Conversation_LastMessage");
                
            entity.HasIndex(e => e.UpdatedAt);
        });

        // ========== CONVERSATION PARTICIPANT CONFIGURATION ==========
        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ConversationParticipants)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LastReadMessage)
                .WithMany()
                .HasForeignKey(e => e.LastReadMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            // CRITICAL: Unique constraint to prevent duplicate participants
            entity.HasIndex(e => new { e.ConversationId, e.UserId })
                .IsUnique()
                .HasDatabaseName("IX_ConversationParticipant_Unique");

            // CRITICAL INDEXES for queries
            entity.HasIndex(e => new { e.UserId, e.IsArchived, e.IsPinned, e.DeletedAt })
                .HasDatabaseName("IX_ConversationParticipant_UserView");
            
            entity.HasIndex(e => new { e.UserId, e.UnreadCount })
                .HasDatabaseName("IX_ConversationParticipant_Unread");
                
            entity.HasIndex(e => e.LastActivityAt);
        });

        // ========== MESSAGE CONFIGURATION ==========
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasDefaultValue(MessageStatus.Sent);
            entity.Property(e => e.Type).IsRequired().HasDefaultValue(MessageType.Text);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);

            // Relationship with Conversation (CRITICAL)
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Group (optional, for backward compatibility)
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Messages)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Reply relationship
            entity.HasOne(e => e.ReplyToMessage)
                .WithMany()
                .HasForeignKey(e => e.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            // CRITICAL INDEXES for message queries
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt, e.IsDeleted })
                .HasDatabaseName("IX_Message_ConversationTimeline");
            
            entity.HasIndex(e => new { e.SenderId, e.CreatedAt })
                .HasDatabaseName("IX_Message_Sender");
            
            entity.HasIndex(e => new { e.GroupId, e.CreatedAt })
                .HasDatabaseName("IX_Message_Group");
                
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
        });

        // ========== MESSAGE ATTACHMENT CONFIGURATION ==========
        modelBuilder.Entity<MessageAttachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FileUrl).IsRequired();

            entity.HasOne(e => e.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.MessageId);
            entity.HasIndex(e => e.ContentType);
        });

        // ========== MESSAGE RECEIPT CONFIGURATION ==========
        modelBuilder.Entity<MessageReceipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId);

            entity.HasOne(e => e.Message)
                .WithMany(m => m.Receipts)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.MessageReceipts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CRITICAL: Unique constraint - one receipt per user per message
            entity.HasIndex(e => new { e.MessageId, e.UserId })
                .IsUnique()
                .HasDatabaseName("IX_MessageReceipt_Unique");

            // CRITICAL INDEXES for receipt queries
            entity.HasIndex(e => new { e.MessageId, e.Status })
                .HasDatabaseName("IX_MessageReceipt_Status");
            
            entity.HasIndex(e => new { e.UserId, e.ReadAt })
                .HasDatabaseName("IX_MessageReceipt_UserRead");
        });

        // ========== GROUP CONFIGURATION ==========
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.GroupId);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.MaxMembers).HasDefaultValue(256);

            entity.HasIndex(e => new { e.IsDeleted, e.CreatedAt });
            entity.HasIndex(e => e.CreatedBy);
        });

        // ========== GROUP MEMBER CONFIGURATION ==========
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JoinedAt).IsRequired();
            entity.Property(e => e.Role).HasDefaultValue(GroupRole.Member);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Group)
                .WithMany(g => g.GroupMembers)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.GroupMembers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AddedByUser)
                .WithMany()
                .HasForeignKey(e => e.AddedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // CRITICAL: Prevent duplicate memberships
            entity.HasIndex(e => new { e.GroupId, e.UserId, e.IsActive })
                .IsUnique()
                .HasDatabaseName("IX_GroupMember_Unique");

            entity.HasIndex(e => new { e.GroupId, e.Role });
            entity.HasIndex(e => e.UserId);
        });

        // ========== EXISTING CONFIGURATIONS (kept for compatibility) ==========
        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.IsVerified).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();

            entity.HasIndex(e => e.Email);
        });

        modelBuilder.Entity<BroadcastMessage>(entity =>
        {
            entity.HasKey(e => e.BroadcastId);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => new { e.SenderId, e.CreatedAt });
        });

        modelBuilder.Entity<BlacklistLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.ActionType).IsRequired();
            entity.Property(e => e.DateAction).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany(u => u.BlacklistLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ActionByUser)
                .WithMany()
                .HasForeignKey(e => e.ActionBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.DateAction);
        });

        modelBuilder.Entity<AuthorizedEmail>(entity =>
        {
            entity.HasKey(e => e.SyncId);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsImported).IsRequired().HasDefaultValue(false);

            entity.HasIndex(e => e.Email).IsUnique();

            entity.ToTable("AuthorizedEmails");
        });

        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.HasIndex(x => new { x.Email, x.Purpose, x.IsUsed, x.ExpiresAtUtc });
        });
    }
}
