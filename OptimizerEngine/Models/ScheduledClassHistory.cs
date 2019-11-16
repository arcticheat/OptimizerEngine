using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class ScheduledClassHistory
    {
        public int ID { get; set; }

        public int ScheduleID { get; set; }

        [NotMapped]
        public String CourseCode { get; set; }

        public int LocationID { get; set; }

        public int RoomID { get; set; }

        public bool Cancelled { get; set; }

        public String Comments { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public String RequestType { get; set; }

        public String Requester { get; set; }

        public bool Hidden { get; set; }

        public DateTime CreationTimestamp { get; set; }

    }
}
