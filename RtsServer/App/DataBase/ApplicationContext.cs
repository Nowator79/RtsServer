using Microsoft.EntityFrameworkCore;
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
            SeedMockDataIfEmpty();
        }

        /// <summary>
        /// В режиме моковой БД при первом обращении добавляет тестового пользователя (test/test), если таблица пуста.
        /// </summary>
        private void SeedMockDataIfEmpty()
        {
            if (!ConfigGameServer.UseMockDatabase)
                return;
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
            }
            else
            {
                optionsBuilder
                    .UseMySql(
                        "server=localhost;user=root;password=root;database=rts_server;",
                        new MySqlServerVersion(new Version(8, 0, 25)))
                    .EnableSensitiveDataLogging();
            }
        }
    }
}
