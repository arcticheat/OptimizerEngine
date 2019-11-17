using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LSS.Models
{
    [Serializable]
    public class OptimizerResult
    {

        //ID
        public int ID { get; set; }

        //Course ID
        public int CourseID { get; set; }

        //Course Code
        [NotMapped]
        public String CourseCode { get; set; }

        //Location ID of course
        public int LocationID { get; set; }

        //Room ID of course
        public int RoomID { get; set; }

        //is class cancelled
        public bool Cancelled { get; set; }

        //comments
        public String Comments { get; set; }

        //What time the class starts 
        public TimeSpan StartTime { get; set; }

        //What time the class ends
        public TimeSpan EndTime { get; set; }

        //Day the class starts 
        public DateTime StartDate { get; set; }

        //Day the class ends
        public DateTime EndDate { get; set; }

        //Type of request
        public String RequestType { get; set; }

        //Requester name
        public String Requester { get; set; }

        //is hidden flag
        public bool Hidden { get; set; }

        //When was this class created 
        public DateTime CreationTimestamp { get; set; }

        //Is the attendance locked
        public bool AttendanceLocked { get; set; }

        //Code why the course is cancelled
        public int? CancelledCodeID { get; set; }

        //Instructor Username
        public String InstrUsername { get; set; }

        public int inputID { get; set; }


        //Course name
        [NotMapped]
        public String Course { get; set; }

        //Location code
        [NotMapped]
        public String Location { get; set; }

        //Room Number 
        [NotMapped]
        public String Room { get; set; }

        //Instructor of the course 
        [NotMapped]
        public User Instructor { get; set; }

        //List of instructors teaching the class
        [NotMapped]
        public User[] InstructorArray { get; set; }

        //Course length in hours
        [NotMapped]
        public int Hours { get; internal set; }

        [NotMapped]
        public bool UsingLocalInstructor { get; set; }
        [NotMapped]
        public string FullName { get; set; }

        public OptimizerResult()
        {

        }
    }
}
