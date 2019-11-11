using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class Room
    {
        //Room ID
        public int ID { get; set; }

        //Where the room is located
        public String Station { get; set; }

        //Room number
        public String Number { get; set; }

        //What building the room is in
        public String Location { get; set; }

        //Owner of the room
        public String Owner { get; set; }

        //Type of room 
        public String RoomType { get; set; }

        //is room active
        public bool Active { get; set; }

        //Notes
        public String Notes { get; set; }

        //List of resources the room has 
        [NotMapped]
        public RoomHasResources[] Resources { get; set; }
        [NotMapped]
        public Dictionary<int, int?> Resources_dict = new Dictionary<int, int?>();
    }

    
}
