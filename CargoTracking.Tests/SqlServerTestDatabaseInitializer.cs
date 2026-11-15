using CargoTracking.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests;
public static class SqlServerTestDatabaseInitializer
{
    public static async Task InitializeAsync()
    {
        await using var context = SqlServerTestDbContextFactory.Create();

        await context.Database.MigrateAsync();
    }
}
