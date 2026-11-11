using CareerGoat.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CareerGoat.UnitTests;

public class UserDataStoreModelTests
{
    [Fact]
    public void DatabaseModel_MapsUserOwnedRecordsToMigrationColumns()
    {
        var options = new DbContextOptionsBuilder<CareerGoatDbContext>()
            .UseNpgsql("Host=localhost;Database=careergoat;Username=test;Password=test")
            .Options;
        using var database = new CareerGoatDbContext(options);
        var profileTable = StoreObjectIdentifier.Table("profiles", "public");
        var roleTable = StoreObjectIdentifier.Table("target_roles", "public");

        Assert.Equal("display_name", database.Model.FindEntityType(typeof(ProfileRecord))!
            .FindProperty(nameof(ProfileRecord.DisplayName))!
            .GetColumnName(profileTable));
        Assert.Equal("user_id", database.Model.FindEntityType(typeof(TargetRoleRecord))!
            .FindProperty(nameof(TargetRoleRecord.UserId))!
            .GetColumnName(roleTable));
        Assert.Equal("updated_at", database.Model.FindEntityType(typeof(TargetRoleRecord))!
            .FindProperty(nameof(TargetRoleRecord.UpdatedAt))!
            .GetColumnName(roleTable));
    }
}
