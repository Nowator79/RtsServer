using Microsoft.EntityFrameworkCore;
using RtsServer.Classes.NetWork.Dto;

namespace RtsServer.Classes.DataBase
{
    public class ApplicationContext : DbContext
    {
        public DbSet<UserAuth> Users { get; set; } = null!;

        public ApplicationContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=localhost;user=root;password=rootpassword;database=rts_server;",
                new MySqlServerVersion(new Version(8, 0, 25)));
        }
    }
}
