using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lok_wss.database.Models
{
    public class kingdomItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Alliance { get; set; }
        public int CastleLevel { get; set; }
        public string Location { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime LastSeen { get; set; }

    }
}
