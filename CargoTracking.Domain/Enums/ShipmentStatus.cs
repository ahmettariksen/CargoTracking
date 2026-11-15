using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Enums;
public enum ShipmentStatus
{
    Created = 1, // kullanıldı
    Accepted = 2, // kullanıldı
    AtWarehouse = 3, // kullanıldı her iki depodada bu kullanılır
    AssignedToCourier = 4, // kullanıldı 2.kurye ataması enumu
    PickedUp = 5, // kullanıldı 2.depodan 2.kurye kargoyu teslim alınca bu enum çalışır
    InTransit = 6, // kullanıldı
    //OutForDelivery = 7,
    Delivered = 8, // kullanıldı
    DeliveryFailed = 9, // kullanıldı
    Cancelled = 10, // kullanıldı
    Returned = 11,

    PickedUpFromSender = 12 // kullanıldı
}
