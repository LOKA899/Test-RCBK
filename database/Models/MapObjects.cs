using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lok_wss.Models
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
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

    public class Param
    {
        public int value { get; set; }
    }

    public class Object
    {
        public string _id { get; set; }
        public List<int> loc { get; set; }
        public int level { get; set; }
        public int code { get; set; }
        public Occupied occupied { get; set; }
        public int state { get; set; }
        public Param param { get; set; }
        public DateTime? expired { get; set; }
    }

    public class Root
    {
        public List<Object> objects { get; set; }
    }


}



