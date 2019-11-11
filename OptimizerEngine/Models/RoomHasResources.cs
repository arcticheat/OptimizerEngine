using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class RoomHasResources
    {
        //ID
        public int ID { get; set; }

        //Room ID
        public int RoomID { get; set; }

        //Resource ID. Resource the room has
        public int ResourceID { get; set; }

        //Amount of resource if applicable
        public int? Amount { get; set; }

        //Description of resource
        [NotMapped]
        public string ResourceDescription { get; set; }
    }
}
