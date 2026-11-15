using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.sda.Exceptions;
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Bu işlemi gerçekleştirmek için yetkiniz yok.")
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
}
