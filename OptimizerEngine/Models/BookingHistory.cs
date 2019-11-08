using System;
using System.Collections.Generic;

namespace OptimizerEngine.Models
{
    public partial class BookingHistory
    {
        public long Id { get; set; }
        public int BookingId { get; set; }
        public string RequestForId { get; set; }
        public string RequestById { get; set; }
        public int? ScheduleId { get; set; }
        public int CodeId { get; set; }
        public string RequestComment { get; set; }
        public string AdminComment { get; set; }
        public DateTime RequestTimestamp { get; set; }
        public DateTime ResponseTimestamp { get; set; }
        public int Status { get; set; }
        public bool Cancelled { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? LocationId { get; set; }
        public TimeSpan? StartTime { get; set; }
    }
}
