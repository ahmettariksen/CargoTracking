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
    private const string ConnectionString = "Server=DESKTOP-VU01UEV\\SQLEXPRESS;Database=CargoTrackingDbContext;Trusted_Connection=True;TrustServerCertificate=True";
    public static CargoTrackingDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CargoTrackingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new CargoTrackingDbContext(options);
    }
}
