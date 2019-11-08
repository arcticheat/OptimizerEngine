using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    public partial class ClassRoster
    {
        public int Id { get; set; }
        public string StudentFileNumber { get; set; }
        public int ScheduledClassId { get; set; }
        public bool Dropped { get; set; }
        public string Comment { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Point { get; set; }
        public bool? Pass { get; set; }
    }
}
