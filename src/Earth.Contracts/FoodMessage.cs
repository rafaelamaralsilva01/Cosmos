using System;

namespace Earth.Contracts
{
    public class FoodMessage
    {
        public FoodMessage(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}
