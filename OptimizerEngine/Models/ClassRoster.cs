using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LSS.Models
{
    public class ClassRoster
    {
        public int ID { get; set; }

        public String StudentFileNumber { get; set; }

        public int ScheduledClassID { get; set; }

        public bool Dropped { get; set; }

        public string Comment { get; set; }

        //Joined
        [NotMapped]
        public String Course { get; set; }       // UA_DEV.dbo.ScheduledClass.CourseCode

        [NotMapped]
        public string Title { get; set; }       // UA_DEV.dbo.Course.Title

        public string LastName { get; set; }    // UA_DEV.dbo.[User].LastName

        public string FirstName { get; set; }   // UA_DEV.dbo.[User].FirstName

        public string Point { get; set; }       // UA_DEV.dbo.[Location].Code as Point,

        public bool Pass { get; set; }

    }
}
