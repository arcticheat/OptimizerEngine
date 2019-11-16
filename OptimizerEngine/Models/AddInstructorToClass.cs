using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    /// <summary>
    /// Model for data needed for the add instructor modal
    /// </summary>
    public class AddInstructorToClass : ScheduledClass
    {
        //Instructor FileNumber
        public string FileNumber { get; set; }

        //Instructor FirstName
        public string FirstName { get; set; }

        //Instructor LastName 
        public string LastName { get; set; }

        //Instructor Middle Initial
        public string MiddleInitial { get; set; }

        //Instructors acitve exceptions
        public Booking[] Exceptions { get; set; }

        //Instructor Location
        public String InstructorLocation { get; set; }

        //Classes that the instructor is teaching
        public ScheduledClass[] ClassesInstructing { get; set; }

        //List of current exception codes the instructor currently has 
        public string[] BookingCodes { get; set; }

    }
}
