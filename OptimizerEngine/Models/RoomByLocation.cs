using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    [NotMapped]
    public class RoomByLocation : Location
    {
        //List of rooms at a location
        [NotMapped]
        public Room[] RoomsArray { get; set; }

    }
}
