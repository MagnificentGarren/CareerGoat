using Microsoft.EntityFrameworkCore;

namespace CareerGoat.Api;

public sealed record ProfileDto(Guid Id, string Email, string? DisplayName, DateTimeOffset UpdatedAt);

public sealed record TargetRoleDto(
    Guid Id,
    string Title,
    string? Company,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpdateProfileRequest(string? DisplayName);

public sealed record TargetRoleRequest(string Title, string? Company, string Description);

public interface IUserDataStore
{
    Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<ProfileDto?> UpdateProfileAsync(Guid userId, string? displayName, CancellationToken cancellationToken);
    Task<IReadOnlyList<TargetRoleDto>> ListTargetRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task<TargetRoleDto?> GetTargetRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);
    Task<TargetRoleDto> CreateTargetRoleAsync(
        Guid userId,
        string title,
        string? company,
        string description,
        CancellationToken cancellationToken);
    Task<TargetRoleDto?> UpdateTargetRoleAsync(
        Guid userId,
        Guid roleId,
        string title,
        string? company,
        string description,
        CancellationToken cancellationToken);
    Task<bool> DeleteTargetRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);
}

public sealed class CareerGoatDbContext(DbContextOptions<CareerGoatDbContext> options) : DbContext(options)
{
    public DbSet<ProfileRecord> Profiles => Set<ProfileRecord>();
    public DbSet<TargetRoleRecord> TargetRoles => Set<TargetRoleRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProfileRecord>(entity =>
        {
            entity.ToTable("profiles", "public");
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.Id).HasColumnName("id");
            entity.Property(profile => profile.Email).HasColumnName("email").IsRequired();
            entity.Property(profile => profile.DisplayName).HasColumnName("display_name").HasMaxLength(120);
            entity.Property(profile => profile.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(profile => profile.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<TargetRoleRecord>(entity =>
        {
            entity.ToTable("target_roles", "public");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(role => role.UserId).HasColumnName("user_id");
            entity.Property(role => role.Title).HasColumnName("title").HasMaxLength(160).IsRequired();
            entity.Property(role => role.Company).HasColumnName("company").HasMaxLength(160);
            entity.Property(role => role.Description).HasColumnName("description").IsRequired();
            entity.Property(role => role.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(role => role.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");
            entity.HasIndex(role => new { role.UserId, role.UpdatedAt })
                .HasDatabaseName("target_roles_user_updated_idx");
        });
    }
}

public sealed class ProfileRecord
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class TargetRoleRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    public string? Company { get; set; }
    public required string Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EfUserDataStore(CareerGoatDbContext database) : IUserDataStore
{
    public async Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await database.Profiles.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken);
        return profile is null ? null : ToDto(profile);
    }

    public async Task<ProfileDto?> UpdateProfileAsync(
        Guid userId,
        string? displayName,
        CancellationToken cancellationToken)
    {
        var profile = await database.Profiles.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken);
        if (profile is null) return null;

        profile.DisplayName = displayName;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return ToDto(profile);
    }

    public async Task<IReadOnlyList<TargetRoleDto>> ListTargetRolesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var roles = await database.TargetRoles
            .Where(role => role.UserId == userId)
            .OrderByDescending(role => role.UpdatedAt)
            .ThenBy(role => role.Id)
            .ToListAsync(cancellationToken);
        return roles.Select(ToDto).ToList();
    }

    public async Task<TargetRoleDto?> GetTargetRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var role = await database.TargetRoles.SingleOrDefaultAsync(
            item => item.UserId == userId && item.Id == roleId,
            cancellationToken);
        return role is null ? null : ToDto(role);
    }

    public async Task<TargetRoleDto> CreateTargetRoleAsync(
        Guid userId,
        string title,
        string? company,
        string description,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var role = new TargetRoleRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Company = company,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now,
        };
        database.TargetRoles.Add(role);
        await database.SaveChangesAsync(cancellationToken);
        return ToDto(role);
    }

    public async Task<TargetRoleDto?> UpdateTargetRoleAsync(
        Guid userId,
        Guid roleId,
        string title,
        string? company,
        string description,
        CancellationToken cancellationToken)
    {
        var role = await database.TargetRoles.SingleOrDefaultAsync(
            item => item.UserId == userId && item.Id == roleId,
            cancellationToken);
        if (role is null) return null;

        role.Title = title;
        role.Company = company;
        role.Description = description;
        role.UpdatedAt = DateTimeOffset.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return ToDto(role);
    }

    public async Task<bool> DeleteTargetRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var role = await database.TargetRoles.SingleOrDefaultAsync(
            item => item.UserId == userId && item.Id == roleId,
            cancellationToken);
        if (role is null) return false;

        database.TargetRoles.Remove(role);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProfileDto ToDto(ProfileRecord profile) =>
        new(profile.Id, profile.Email, profile.DisplayName, profile.UpdatedAt);

    private static TargetRoleDto ToDto(TargetRoleRecord role) =>
        new(role.Id, role.Title, role.Company, role.Description, role.CreatedAt, role.UpdatedAt);
}
