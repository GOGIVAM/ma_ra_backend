using MaRa.Api.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MaRa.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Equipement> Equipements => Set<Equipement>();
    public DbSet<Gamme> Gammes => Set<Gamme>();
    public DbSet<Etape> Etapes => Set<Etape>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<InterventionLog> InterventionLogs => Set<InterventionLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Equipement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Classe);
            e.HasMany(x => x.Gammes).WithOne(g => g.Equipement).HasForeignKey(g => g.EquipementId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Documents).WithOne(d => d.Equipement).HasForeignKey(d => d.EquipementId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Gamme>(g =>
        {
            g.HasKey(x => x.Id);
            g.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            g.HasIndex(x => x.Code).IsUnique();
            g.HasMany(x => x.Etapes).WithOne(e => e.Gamme).HasForeignKey(e => e.GammeId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Etape>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            e.HasIndex(x => new { x.GammeId, x.OrdreIndex });
        });

        builder.Entity<Document>(d =>
        {
            d.HasKey(x => x.Id);
            d.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        builder.Entity<InterventionLog>(il =>
        {
            il.HasKey(x => x.Id);
            il.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
            il.HasIndex(x => x.UserId);
            il.HasIndex(x => x.DebutAt);
        });
    }
}
