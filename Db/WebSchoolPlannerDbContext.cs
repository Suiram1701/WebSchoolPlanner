using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using WebSchoolPlanner.Db.Models;

namespace WebSchoolPlanner.Db;

public class WebSchoolPlannerDbContext : IdentityDbContext<User, Role, string>
{
    public DbSet<UserImageModel> UserImages { get; set; }

    public WebSchoolPlannerDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>()     // Configure user image
            .HasOne(u => u.AccountImage)
            .WithOne()
            .HasForeignKey<UserImageModel>(ui => ui.Id)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
