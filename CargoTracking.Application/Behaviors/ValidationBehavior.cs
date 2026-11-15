using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Behaviors;

//FluentValidation kuralları yazan araçtır; ValidationBehavior ise bu kuralların MediatR üzerinden otomatik çalışmasını sağlayan mekanizmadır.

//Bu kod, CargoTracking projemizde MediatR ile çalışan komut ve sorguların handler'a ulaşmadan önce otomatik olarak doğrulanmasını sağlar. Buna Validation Pipeline Behavior denir.
//Örneğin CreateShipmentCommand gönderildiğinde kargonun ağırlığı sıfırdan büyük mü, gönderici ile alıcı aynı kişi mi gibi kurallar kontrol edilir.İstek geçersizse handler çalıştırılmaz.

//public: Sınıfın diğer katmanlardan erişilebilir olmasını sağlar.
//sealed: Bu sınıftan başka bir sınıf türetilmesini engeller.
//ValidationBehavior: Sınıfımızın adı.Doğrulama davranışını temsil eder.
//TRequest: MediatR'a gönderilen isteğin türüdür. Örneğin CreateShipmentCommand.
//TResponse: Handler'ın döndürdüğü cevabın türüdür. Örneğin Guid.
//IPipelineBehavior<TRequest, TResponse>: MediatR'ın, handler çalışmadan önce veya sonra araya giren işlemler tanımlamamızı sağlayan arayüzüdür.
//where TRequest : notnull: TRequest türünün null olamayacağını belirtir

//Buradaki TRequest ve TResponse sayesinde aynı sınıfı farklı komutlar ve farklı dönüş tipleri için tekrar kullanabiliriz.
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if(!_validators.Any())
        {
            return await next();
        }
        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(validator =>
                validator.ValidateAsync(context, cancellationToken)));

        // Hataları tek listede toplama
        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }
        return await next();
    }
}
