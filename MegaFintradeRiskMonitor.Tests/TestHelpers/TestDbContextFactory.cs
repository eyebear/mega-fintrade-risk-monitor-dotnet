using MegaFintradeRiskMonitor.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MegaFintradeRiskMonitor.Tests.TestHelpers;

public static class TestDbContextFactory
{
    public static async Task<RiskMonitorDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<RiskMonitorDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new RiskMonitorDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        return dbContext;
    }
}