using System;
using System.Collections.Generic;
using System.Text;

namespace LSS.Models
{
    class OptimizerResultPrintable
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

        public string Instructor { get; set; }
        

        public OptimizerResultPrintable(OptimizerResult result)
        {
            ID = result.ID;
            CourseCode = result.CourseCode;
            LocationCode = result.Location;
            RoomID = result.RoomID;
            StartTime = result.StartTime;
            EndTime = result.EndTime;
            StartDate = result.StartDate;
            EndDate = result.EndDate;
            Instructor = result.InstrUsername;
        }
    }
}
