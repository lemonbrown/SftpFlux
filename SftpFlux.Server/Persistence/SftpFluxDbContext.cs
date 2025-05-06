using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SftpFlux.Server.Authorization;
using SftpFlux.Server.Connection;
using System.Text.Json;

namespace SftpFlux.Server.Persistence {

    public class SftpFluxDbContext : DbContext {
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<SftpConnectionInfo> SftpConfigs { get; set; }
        public DbSet<SftpMetadataEntry> CachedEntries { get; set; }

        public SftpFluxDbContext(DbContextOptions<SftpFluxDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {

            modelBuilder.Entity<ApiKey>().HasKey(k => k.Key);

            modelBuilder.Entity<SftpMetadataEntry>().HasIndex(c => c.Path);
            modelBuilder.Entity<SftpMetadataEntry>().HasNoKey();
            modelBuilder.Entity<SftpConnectionInfo>().HasKey(c => c.Id);

            var converter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

            modelBuilder.Entity<ApiKey>()
                .Property(e => e.Scopes)
                .HasConversion(converter);
        }        
    }
}
