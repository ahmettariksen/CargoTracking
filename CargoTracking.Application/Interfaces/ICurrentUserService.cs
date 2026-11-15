using CargoTracking.Application.Features.Shipments.CreateShipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Interfaces;

// Şu anda CreateShipmentCommand içinde SenderId istemciden geliyor. Örneğin, giriş yapan kullanıcının ID'si A olsun. Swagger üzerinden SenderId alanına başka bir kullanıcının ID'si olan B yazabilir. Handler, B kullanıcısının aktif olduğunu görürse mevcut kontrollerden geçebilir. Bu durumu engelleyeceğiz a kullanıcısı b nin ıdsini girerek kargo yollayamayacak.

//Bu arayüzün görevi: Handler'ların, isteği yapan kullanıcının ID'sini alabilmesini sağlamak. ID'yi daha sonra API katmanında doğrulanmış JWT token'ından okuyacağız.

//Bunu Controller'da da yapabiliriz. Ancak mevcut projemizde Clean Architecture kullanıyoruz ve iş mantığı Application katmanındaki handler'larda bulunuyor.

//Nasıl çalışacak
//1. Kullanıcı giriş yapar
//API bir JWT token üretir.


//2. ICurrentUserService
//Token'daki kullanıcı ID'sini sunucu tarafında okur.


//3. CreateShipmentCommandHandler
//Giriş yapan kullanıcının ID'sini gönderici ID'siyle karşılaştırır.

//4. Id eşlerşirse devam eşleşmez ise istek reddedilir.
public interface ICurrentUserService
{
    Guid? UserId { get; }
}
