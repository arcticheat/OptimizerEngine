﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class InstructorOfClass
    {
        //ID
        public int ID { get; set; }
        
        //Instructor fileNumber
        public String UserID { get; set; }

        //Scheduled Class ID
        public int ClassID { get; set; }

        //Start Time
        public TimeSpan StartTime { get; set; }

        //Start Date
        public DateTime StartDate { get; set; }

        //End Date
        public DateTime EndDate { get; set; }

        //Cancelled flag
        public bool Cancelled { get; set; }

        //Hidden flag 
        public bool Hidden { get; set; }
        [NotMapped]
        public bool LocalAssignment { get; set; }
    }
}
