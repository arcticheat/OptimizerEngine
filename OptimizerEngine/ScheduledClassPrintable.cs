using OptimizerEngine.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OptimizerEngine
{
    class ScheduledClassPrintable 
    {
        //ID
        public int ID { get; set; }

        //Course ID
        public int CourseID { get; set; }

        //Location ID of course
        public int LocationID { get; set; }

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

        public ScheduledClassPrintable(ScheduledClass scheduled)
        {
            ID = scheduled.ID;
            CourseID = scheduled.CourseID;
            LocationID = scheduled.LocationID;
            RoomID = scheduled.RoomID;
            StartTime = scheduled.StartTime;
            EndTime = scheduled.EndTime;
            StartDate = scheduled.StartDate;
            EndDate = scheduled.EndDate;
            Hidden = scheduled.Hidden;

        }
    }
}
