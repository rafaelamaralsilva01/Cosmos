using Shuttle.Bus;
using System;

namespace Earth.Contracts
{
    public class AskForTechMessage : IntegrationEvent
    {
        public AskForTechMessage(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}
