using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Satellite.Aggregates.Food
{
    public class SendFoodHandler
    {
        public Task Handle(dynamic message)
        {
            return Task.CompletedTask;
        }
    }
}
