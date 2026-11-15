using CargoTracking.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Fakes;
public sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }
}
