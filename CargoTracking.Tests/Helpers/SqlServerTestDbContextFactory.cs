using CargoTracking.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Helpers;
public static class SqlServerTestDbContextFactory
{
    private const string LocalConnectionString =
         "Server=DESKTOP-VU01UEV\\SQLEXPRESS;Database=CargoTrackingDbContext;Trusted_Connection=True;TrustServerCertificate=True";
    public static CargoTrackingDbContext Create()
    {
        var connectionString =
           Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION_STRING")
           ?? LocalConnectionString;

        var options = new DbContextOptionsBuilder<CargoTrackingDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new CargoTrackingDbContext(options);
    }
}
