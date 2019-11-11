using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class Location
    {
        //ID
        public int ID { get; set; }

        //Location Code
        public String Code { get; set; }

        //is point flag
        public bool Point { get; set; }

        //Latitude
        public double Latitude { get; set; }

        //Longitude 
        public double Longitude { get; set; }

        //Release rate for the location
        public int ReleaseRate { get; set; }
        [NotMapped]
        public List<int> LocalRooms { get; set; } = new List<int>();
        [NotMapped]
        public List<string> LocalInstructors { get; set; } = new List<string>();
      
        public bool HasLocalRooms()
        {
            return 0 < LocalRooms.Count;
        }

        public bool HasLocalInstructors()
        {
            return 0 < LocalInstructors.Count;
        }
    }
}
