
using System;
using System.Collections.Generic;

namespace lok_wss.database.Models
{
    public class Occupied
    {
        public string id { get; set; }
        public DateTime started { get; set; }
        public object skin { get; set; }
        public string name { get; set; }
        public int worldId { get; set; }
        public string allianceId { get; set; }
        public string allianceTag { get; set; }
    }
}
