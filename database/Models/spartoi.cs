using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lok_wss.database.Models
{
    public class Spartoi
    {
        public Guid uguid { get; set; }
        public string id { get; set; }
        public string location { get; set; }
        public string continent { get; set; }
        public DateTime found { get; set; }
        
    }
}
