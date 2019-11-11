using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LSS.Models
{
    public class Attendance
    {
        public long ID { get; set; }

        public int ClassRosterID { get; set; }

        public int SubjectNumber { get; set; }

        public int Hours { get; set; }
    }

    public class AttendanceDto
    {
        public long ID { get; set; }
        
        public int ClassRosterID { get; set; }

        public string StudentFileNumber { get; set; }

        public string StudentName { get; set; }

        public string StudentPoint { get; set; }

        public string Comment { get; set; }

        public Attendance[] SubjectHours { get; set; }

        public bool Pass { get; internal set; }
    }

    public class MobileAttendance
    {
        public int ScheduleID { get; set; }

        public int ClassRosterID { get; set; }

        public long AttendanceID { get; set; }

        public int SubjectNumber { get; set; }

        public string StudentFileNumber { get; set; }

        public string StudentFirstName { get; set; }

        public string StudentLastName { get; set; }

        public int HoursAttended { get; set; }

    }

    //public class SubjectHour
    //{
    //    public int Subject { get; set; }

    //    public int Hours { get; set; }
    //}
}
