using System;
using System.Collections.Generic;
using System.Text;

namespace OptimizerEngine.Models
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

    }
}
