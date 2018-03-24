using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Earth.Aggregates.Food
{
    public class SendFoodMessage
    {
        public SendFoodMessage(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}