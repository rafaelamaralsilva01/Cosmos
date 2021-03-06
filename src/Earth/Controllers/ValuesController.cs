﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Earth.Aggregates.Food;
using Microsoft.AspNetCore.Mvc;

namespace Earth.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly SendFoodSender sender;
        public ValuesController(SendFoodSender sender)
        {
            this.sender = sender;
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            sender.Send();
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
