using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Satellite.Aggregates.Technologies
{
    public class TechnologiesRepository
    {
        public static ConcurrentBag<Tech> techs = new ConcurrentBag<Tech>();

        public void Store(Tech item)
        {
            if (!techs.Any(x => x.Name == item.Name))
                techs.Add(item);
        }

        public Tech FindByName(string name)
        {
            return techs.SingleOrDefault(tech => tech.Name == name);
        }

        public IEnumerable<Tech> Read()
        {
            return techs.AsEnumerable();
        }
    }
}
