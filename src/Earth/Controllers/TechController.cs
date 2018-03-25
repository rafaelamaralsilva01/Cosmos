using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Earth.Aggregates.Technologies;
using Microsoft.AspNetCore.Mvc;
using Shuttle.Bus;

namespace Earth.Controllers
{
    [Route("api/[controller]")]
    public class TechController : Controller
    {
        private readonly IShuttle bus;

        public TechController(IShuttle bus)
        {
            this.bus = bus;
        }

        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public Task<IEnumerable<string>> Get()
        {
            var allTechsCommand = new AskForAllTechsCommand();
            bus.Publish(allTechsCommand);

            return Task.FromResult(new List<string> { "value1", "value2" }.AsEnumerable());
        }
    }
}