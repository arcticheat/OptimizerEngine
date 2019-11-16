using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSS.Models
{
    public class ScheduleCalendarEvent
    {
        public int Id { get; set; }

        public string Resource { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public string Text { get; set; }

        public string Color { get; set; }

        public string BubbleHtml { get; set; }

    }

    public class MonthlyCalendarEvent
    {
        public int Id { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public string Text { get; set; }

        public string BubbleHtml { get; set; }

        public string BackColor { get; set; }
    }
}
