using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizerEngine.Models
{
    public partial class Location
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public bool Point { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int? ReleaseRate { get; set; }
        [NotMapped]
        public List<int> LocalRooms { get; set; } = new List<int>();
        [NotMapped]
        public List<string> LocalInstructors { get; set; } = new List<string>();
        [NotMapped]
        public Dictionary<string, List<string>> LocallyTaughtCoursesPerDay = new Dictionary<string, List<string>>();
      
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
