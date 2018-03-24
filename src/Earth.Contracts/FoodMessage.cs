using Shuttle.Bus;
using System;

namespace Earth.Contracts
{
    public class FoodMessage : IntegrationEvent
    {
        public FoodMessage(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}
