using System;

namespace Satellite.Aggregates.Technologies
{
    public class Tech
    {
        public Tech()
        {
            this.CreatedDate = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Area { get; set; }

        public DateTime CreatedDate { get; }
    }
}
