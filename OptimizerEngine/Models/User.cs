using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    [Serializable]
    public class User
    {
        //User Username
        public String Username { get; set; }

        //User Password
        public String Password { get; set; }

        //User role ID
        public int RoleID { get; set; }

        //Last Name
        public String LastName { get; set; }

        //First Name
        public String FirstName { get; set; }

        //Middle Initial
        public String MiddleInitial { get; set; }

        //ID of user's supervisor
        public String SupervisorID { get; set; }
        
        //User's FileNumber
        [Key]
        public String FileNumber { get; set; }

        //User's point ID
        public int PointID { get; set; }

        //Point code
        [NotMapped]
        public String Point { get; set; }

        //Status ID
        [NotMapped]
        public int StatusID { get; set; }

        //Status
        [NotMapped]
        public String Status { get; set; }

        //User's supervisor Name
        [NotMapped]
        public string SupervisorName { get; internal set; }

        //Start Date
        [NotMapped]
        public DateTime StartDate { get; set; }

        //End Date
        [NotMapped]
        public DateTime EndDate { get; set; }

        //User's Role
        [NotMapped]
        public string Role { get; set; }
        [NotMapped] 
        public int QualificationCount { get; set; }

        //Point in string form
        [NotMapped]
        public string PointName { get; set; }

    }

    /// <summary>
    /// DTO used for authenticating login
    /// </summary>
    public class UserDTO
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
