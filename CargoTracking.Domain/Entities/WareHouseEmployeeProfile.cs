using CargoTracking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;
public sealed class WareHouseEmployeeProfile:BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid WareHouseId { get; private set; }
    public User User { get; private set; } = default!;
    public Warehouse Warehouse { get; private set; } = default!;

    private WareHouseEmployeeProfile()
    {
    }
    public WareHouseEmployeeProfile(Guid userId,Guid warehouseId)
    {
        UserId = userId;
        WareHouseId = warehouseId;
    }
}
