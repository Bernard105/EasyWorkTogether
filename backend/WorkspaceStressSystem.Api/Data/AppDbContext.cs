using Microsoft.EntityFrameworkCore;
using WorkspaceStressSystem.Api.Models.Entities;
using WorkspaceStressSystem.Api.Models.Enums;

namespace WorkspaceStressSystem.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<WorkspaceInvitation> WorkspaceInvitations => Set<WorkspaceInvitation>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Avatar).HasColumnName("avatar");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.ToTable("workspaces");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb");
            entity.Property(x => x.OwnerId).HasColumnName("owner_id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(x => x.Owner)
                .WithMany(x => x.OwnedWorkspaces)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.ToTable("workspace_members");
            entity.HasKey(x => new { x.WorkspaceId, x.UserId });

            entity.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.Role)
                .HasColumnName("role")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => v.ToLower() == "owner"
                        ? WorkspaceRole.Owner
                        : v.ToLower() == "admin"
                            ? WorkspaceRole.Admin
                            : WorkspaceRole.Member);
            entity.Property(x => x.JoinedAt).HasColumnName("joined_at");

            entity.HasOne(x => x.Workspace)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.WorkspaceMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.AccessToken).HasColumnName("access_token").HasMaxLength(512).IsRequired();
            entity.Property(x => x.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(512).IsRequired();
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => v.ToLower() == "active"
                        ? SessionStatus.Active
                        : SessionStatus.Revoked);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasOne(x => x.User)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkspaceInvitation>(entity =>
        {
            entity.ToTable("workspace_invitations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
            entity.Property(x => x.InviterId).HasColumnName("inviter_id");
            entity.Property(x => x.InviteeEmail).HasColumnName("invitee_email").HasMaxLength(255).IsRequired();
            entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => v.ToLower() == "pending"
                        ? InvitationStatus.Pending
                        : v.ToLower() == "accepted"
                            ? InvitationStatus.Accepted
                            : v.ToLower() == "expired"
                                ? InvitationStatus.Expired
                                : v.ToLower() == "rejected"
                                    ? InvitationStatus.Rejected
                                    : InvitationStatus.Pending);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(x => x.Code).IsUnique();

            entity.HasOne(x => x.Workspace)
                .WithMany(x => x.Invitations)
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Inviter)
                .WithMany(x => x.SentInvitations)
                .HasForeignKey(x => x.InviterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.IsUsed).HasColumnName("is_used");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}