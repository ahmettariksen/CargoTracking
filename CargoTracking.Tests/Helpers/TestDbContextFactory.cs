using CargoTracking.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Helpers;

//Bu kodun amacı:
//- UseInMemoryDatabase: SQL Server yerine bellekte çalışan geçici bir veritabanı kullanır.
//- Guid.NewGuid().ToString() : Her çağrıda benzersiz bir veritabanı adı üretir.Böylece testler birbirlerinin verilerini kullanmaz.
//- CargoTrackingDbContext: Normal uygulamada kullandığın veritabanı bağlamını test ortamında oluşturur.
public static class TestDbContextFactory
{
    public static CargoTrackingDbContext Create()
    {
        var options = new DbContextOptionsBuilder<CargoTrackingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CargoTrackingDbContext(options);
    }
}
