using GestionalePersone.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionalePersone.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Persona> Persone => Set<Persona>();
}
