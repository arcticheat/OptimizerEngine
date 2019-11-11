using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class ClassSubjectAttendance
    {
        //ID
        int ID { get; set; }

        //Roster ID
        int ClassRosterID { get; set; }

        //Subject Number
        int SubjectNumber { get; set; }

        //Hours for subject number 
        int Hours { get; set; }
    }
}
