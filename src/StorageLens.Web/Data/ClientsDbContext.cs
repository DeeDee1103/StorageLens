using Microsoft.EntityFrameworkCore;
using StorageLens.Shared.Infrastructure;

namespace StorageLens.Web.Data;

public class ClientsDbContext(DbContextOptions<ClientsDbContext> options) : DbContext(options)
{
    public DbSet<ClientRecord> Clients => Set<ClientRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SqlSchemas.Clients);

        modelBuilder.Entity<ClientRecord>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AccountNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Industry).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Website).HasMaxLength(300).IsRequired();

            entity.Property(x => x.ContactFirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ContactLastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ContactEmail).HasMaxLength(254).IsRequired();
            entity.Property(x => x.ContactPhone).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ContactTitle).HasMaxLength(100).IsRequired();

            entity.Property(x => x.BillingAddress).HasMaxLength(300).IsRequired();
            entity.Property(x => x.City).HasMaxLength(100).IsRequired();
            entity.Property(x => x.State).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(100).IsRequired();

            entity.Property(x => x.AssignedTierId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AssignedServiceIdsJson).HasMaxLength(2000).IsRequired();

            entity.Property(x => x.NotificationEmails).HasMaxLength(1000).IsRequired();

            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000).IsRequired();

            entity.HasIndex(x => x.AccountNumber).IsUnique();
            entity.HasIndex(x => x.CompanyName);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
        });
    }
}
