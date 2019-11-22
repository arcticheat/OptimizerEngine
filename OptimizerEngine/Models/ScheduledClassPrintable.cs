using System;
using System.Collections.Generic;
using System.Text;

namespace LSS.Models
{
    class ScheduledClassPrintable 
    {
        //ID
        public int ID { get; set; }

        //Course ID
        public string CourseCode { get; set; }

        //Location ID of course
        public string LocationCode { get; set; }

        //Room ID of course
        public int RoomID { get; set; }


        //What time the class starts 
        public TimeSpan StartTime { get; set; }

        //What time the class ends
        public TimeSpan EndTime { get; set; }

        //Day the class starts 
        public DateTime StartDate { get; set; }

        //Day the class ends
        public DateTime EndDate { get; set; }

        //is hidden flag
        public bool Hidden { get; set; }

        public string Instructor { get; set; }

        public string InstructorLastname { get; set; }


        public ScheduledClassPrintable(ScheduledClass scheduled)
        {
            ID = scheduled.ID;
            CourseCode = scheduled.CourseCode;
            LocationCode = scheduled.Location;
            RoomID = scheduled.RoomID;
            StartTime = scheduled.StartTime;
            EndTime = scheduled.EndTime;
            StartDate = scheduled.StartDate;
            EndDate = scheduled.EndDate;
            Hidden = scheduled.Hidden;
            Instructor = scheduled.Instructor.Username;
            InstructorLastname = scheduled.Instructor.LastName;
        }
    }
}
