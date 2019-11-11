using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class ScheduleCalendarResource
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public bool Expanded = true;

        public ScheduleCalendarResourceChild[] Children { get; set; }
    }

    public class ScheduleCalendarResourceChild
    {
        public string Name { get; set; }

        public string Id { get; set; }
    }
}
