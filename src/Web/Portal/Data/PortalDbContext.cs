// <copyright file="PortalDbContext.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Data;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// A lightweight database context which maps only the tables and columns the portal needs.
/// </summary>
/// <remarks>
/// The schema is owned and migrated by the game server (<c>EntityDataContext</c>).
/// This context never creates or migrates anything; the mapping is verified against
/// the server model by a unit test, so schema changes break the build instead of the site.
/// </remarks>
public class PortalDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PortalDbContext"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    public PortalDbContext(DbContextOptions<PortalDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the accounts.
    /// </summary>
    public DbSet<AccountRow> Accounts => this.Set<AccountRow>();

    /// <summary>
    /// Gets the characters.
    /// </summary>
    public DbSet<CharacterRow> Characters => this.Set<CharacterRow>();

    /// <summary>
    /// Gets the stat attributes of characters and accounts.
    /// </summary>
    public DbSet<StatAttributeRow> StatAttributes => this.Set<StatAttributeRow>();

    /// <summary>
    /// Gets the character classes.
    /// </summary>
    public DbSet<CharacterClassRow> CharacterClasses => this.Set<CharacterClassRow>();

    /// <summary>
    /// Gets the guilds.
    /// </summary>
    public DbSet<GuildRow> Guilds => this.Set<GuildRow>();

    /// <summary>
    /// Gets the guild members.
    /// </summary>
    public DbSet<GuildMemberRow> GuildMembers => this.Set<GuildMemberRow>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AccountRow>(b =>
        {
            b.ToTable("Account", "data", t => t.ExcludeFromMigrations());
            b.Property(a => a.LoginName).HasMaxLength(10);
            b.Property(a => a.LanguageIsoCode).HasMaxLength(3);
        });

        modelBuilder.Entity<CharacterRow>(b =>
        {
            b.ToTable("Character", "data", t => t.ExcludeFromMigrations());
            b.Property(c => c.Name).HasMaxLength(10);
        });

        modelBuilder.Entity<StatAttributeRow>().ToTable("StatAttribute", "data", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<CharacterClassRow>().ToTable("CharacterClass", "config", t => t.ExcludeFromMigrations());

        modelBuilder.Entity<GuildRow>(b =>
        {
            b.ToTable("Guild", "guild", t => t.ExcludeFromMigrations());
            b.Property(g => g.Name).HasMaxLength(8);
        });

        modelBuilder.Entity<GuildMemberRow>().ToTable("GuildMember", "guild", t => t.ExcludeFromMigrations());
    }
}
