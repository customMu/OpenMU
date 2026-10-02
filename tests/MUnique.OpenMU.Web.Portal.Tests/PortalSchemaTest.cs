// <copyright file="PortalSchemaTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Tests;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.EntityFramework;
using MUnique.OpenMU.Web.Portal.Data;

/// <summary>
/// Verifies that the mapping of the <see cref="PortalDbContext"/> matches the schema
/// which is owned by the game server (<see cref="EntityDataContext"/>).
/// </summary>
[TestFixture]
public class PortalSchemaTest
{
    private IModel _serverModel = null!;
    private IModel _portalModel = null!;

    /// <summary>
    /// Builds both models. No database connection is required.
    /// </summary>
    [OneTimeSetUp]
    public void SetUp()
    {
        using var serverContext = new EntityDataContext();
        this._serverModel = serverContext.GetService<IDesignTimeModel>().Model;

        var options = new DbContextOptionsBuilder<PortalDbContext>().UseNpgsql("Host=localhost").Options;
        using var portalContext = new PortalDbContext(options);
        this._portalModel = portalContext.GetService<IDesignTimeModel>().Model;
    }

    /// <summary>
    /// Tests that every mapped column exists in the server schema with the same type and nullability.
    /// </summary>
    [Test]
    public void MappedColumnsExistInServerSchema()
    {
        Assert.Multiple(() =>
        {
            foreach (var portalEntity in this._portalModel.GetEntityTypes())
            {
                var table = StoreObjectIdentifier.Table(portalEntity.GetTableName()!, portalEntity.GetSchema());
                var serverEntity = this.FindServerEntity(table);
                Assert.That(serverEntity, Is.Not.Null, $"Table {table.DisplayName()} doesn't exist in the server schema.");
                if (serverEntity is null)
                {
                    continue;
                }

                foreach (var portalProperty in portalEntity.GetProperties())
                {
                    var columnName = portalProperty.GetColumnName(table)!;
                    var serverProperty = serverEntity.GetProperties().FirstOrDefault(p => p.GetColumnName(table) == columnName);
                    Assert.That(serverProperty, Is.Not.Null, $"Column {table.DisplayName()}.{columnName} doesn't exist in the server schema.");
                    if (serverProperty is null)
                    {
                        continue;
                    }

                    Assert.That(portalProperty.GetColumnType(table), Is.EqualTo(serverProperty.GetColumnType(table)), $"Type of {table.DisplayName()}.{columnName}");
                    Assert.That(portalProperty.IsColumnNullable(table), Is.EqualTo(serverProperty.IsColumnNullable(table)), $"Nullability of {table.DisplayName()}.{columnName}");
                }
            }
        });
    }

    /// <summary>
    /// Tests that the portal maps all columns which are required to insert a new account.
    /// </summary>
    [Test]
    public void AccountInsertProvidesAllRequiredColumns()
    {
        var table = StoreObjectIdentifier.Table("Account", "data");
        var serverEntity = this.FindServerEntity(table)!;
        var portalEntity = this._portalModel.FindEntityType(typeof(AccountRow))!;
        var portalColumns = portalEntity.GetProperties().Select(p => p.GetColumnName(table)).ToHashSet();

        var requiredColumns = serverEntity.GetProperties()
            .Where(p => !p.IsColumnNullable(table) && p.GetDefaultValue(table) is null && p.GetDefaultValueSql(table) is null)
            .Select(p => p.GetColumnName(table))
            .ToList();

        Assert.That(portalColumns, Is.SupersetOf(requiredColumns));
    }

    /// <summary>
    /// Tests that the attribute definition ids match the ones of the game logic.
    /// </summary>
    [Test]
    public void AttributeDefinitionIdsMatchGameLogic()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StatAttributeRow.LevelId, Is.EqualTo(Stats.Level.Id));
            Assert.That(StatAttributeRow.MasterLevelId, Is.EqualTo(Stats.MasterLevel.Id));
            Assert.That(StatAttributeRow.ResetsId, Is.EqualTo(Stats.Resets.Id));
        });
    }

    private IEntityType? FindServerEntity(StoreObjectIdentifier table)
    {
        return this._serverModel.GetEntityTypes()
            .FirstOrDefault(e => e.GetTableName() == table.Name && e.GetSchema() == table.Schema);
    }
}
