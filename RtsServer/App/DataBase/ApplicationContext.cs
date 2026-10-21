using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RtsServer.App;
using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.DataBase
{
    public class ApplicationContext : DbContext
    {
        public DbSet<UserAuth> Users { get; set; } = null!;

        public ApplicationContext()
        {
            Database.EnsureCreated();
            EnsureAuthTokenColumn();
            SeedIfEmpty();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAuth>(e =>
            {
                e.ToTable("Users");
                e.HasKey(x => x.Id);
                e.Property(x => x.UserName).HasMaxLength(64).IsRequired();
                e.Property(x => x.Password).HasMaxLength(128).IsRequired();
                e.Property(x => x.AuthToken).HasMaxLength(64);
                e.HasIndex(x => x.UserName).IsUnique();
                e.HasIndex(x => x.AuthToken);
            });
        }

        /// <summary>
        /// Старая таблица Users могла быть без AuthToken — добавляем колонку, если её нет.
        /// </summary>
        private void EnsureAuthTokenColumn()
        {
            if (ConfigGameServer.UseMockDatabase)
                return;

            try
            {
                Database.ExecuteSqlRaw(
                    """
                    ALTER TABLE Users
                    ADD COLUMN IF NOT EXISTS AuthToken VARCHAR(64) NULL;
                    """);
            }
            catch
            {
                // MySQL < 8.0.29 без IF NOT EXISTS — пробуем обычный ADD и игнорируем «duplicate column».
                try
                {
                    Database.ExecuteSqlRaw(
                        "ALTER TABLE Users ADD COLUMN AuthToken VARCHAR(64) NULL;");
                }
                catch
                {
                    // колонка уже есть
                }
            }
        }

        private void SeedIfEmpty()
        {
            if (Users.Any())
                return;

            Users.Add(new UserAuth("test", "test"));
            SaveChanges();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (ConfigGameServer.UseMockDatabase)
            {
                optionsBuilder.UseInMemoryDatabase("RtsServer_MockDb");
                return;
            }

            optionsBuilder.UseMySql(
                ConfigGameServer.MySqlConnectionString,
                new MySqlServerVersion(new Version(8, 0, 25)));
        }

        /// <summary>Проверка доступности БД при старте сервера.</summary>
        public static void WarmUp(ILogger? logger = null)
        {
            try
            {
                using var db = new ApplicationContext();
                int count = db.Users.Count();
                logger?.LogInformation(
                    "БД готова ({Mode}). Пользователей: {Count}",
                    ConfigGameServer.UseMockDatabase ? "mock/in-memory" : "MySQL",
                    count);
            }
            catch (Exception ex)
            {
                logger?.LogError(
                    ex,
                    "Не удалось открыть БД. Mode={Mode}, Conn={Conn}. " +
                    "Подними MySQL и примени Docs/mysql_init.sql, либо RTS_USE_MOCK_DB=1.",
                    ConfigGameServer.UseMockDatabase ? "mock" : "MySQL",
                    ConfigGameServer.UseMockDatabase ? "(in-memory)" : ConfigGameServer.MySqlConnectionString);
                throw;
            }
        }
    }
}
