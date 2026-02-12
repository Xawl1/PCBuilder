using Microsoft.EntityFrameworkCore;
using PCBuilder.Models;

namespace PCBuilder.Data
{

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        //в базата ще се създаде след миграция и ъпдейт таблица Users
        public DbSet<User> Users { get; set; }
    }

}
