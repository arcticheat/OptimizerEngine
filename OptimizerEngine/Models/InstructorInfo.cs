using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class InstructorInfo
    {
        //Instructor FileNumber
        public string FileNumber { get; set; }

        //Instructor FirstName
        public string FirstName { get; set; }

        //Instructor LastName 
        public string LastName { get; set; }

        //Instructor Middle Initial
        public string MiddleInitial { get; set; }

        //Instructor Location
        public String InstructorLocation { get; set; }

        public Booking[] Exceptions { get; set; }

        public bool Available { get; set; }

        public string LastTaughtDate { get; set; }

        public string LastTaughtCourseDate { get; set; }
        
        public string Qualification { get; set; }

        public DateTime StartDateString { get; set; }
        public DateTime EndDateString { get; set; }

    }
}
