using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Events;

//Bu olay, kuryeye gönderilecek e-postanın ihtiyaç duyduğu verileri taşıyacak.
public sealed record CourierAssignmentEmailEvent(
    string Email,
    string TrackingNumber,
    string Address,
    string WarehouseName,
    string WarehouseAddress);
