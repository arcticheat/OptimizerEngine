using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    [Serializable]
    public partial class User
    {
        public string FileNumber { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int RoleId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleInitial { get; set; }
        public string SupervisorId { get; set; }
        public int PointId { get; set; }
    }
}
