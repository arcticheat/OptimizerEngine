﻿using OptimizerEngine.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OptimizerEngine
{
    class OptimizerResultPrintable
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

        public string Instructor { get; set; }

        public OptimizerResultPrintable(OptimizerResult result)
        {
            ID = result.ID;
            CourseID = result.CourseID;
            LocationID = result.LocationID;
            RoomID = result.RoomID;
            StartTime = result.StartTime;
            EndTime = result.EndTime;
            StartDate = result.StartDate;
            EndDate = result.EndDate;
            Instructor = result.InstrUsername;
        }
    }
}
