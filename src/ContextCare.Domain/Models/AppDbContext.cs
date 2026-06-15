using System;
using Microsoft.EntityFrameworkCore;

namespace ContextCare.Domain.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
    }

    public DbSet<KnowledgeBase> KnowledgeBases { get; set; }
}
