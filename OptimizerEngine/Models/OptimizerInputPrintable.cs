using LSS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace LSS.Models
{
    class OptimizerInputPrintable
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public String CourseCode { get; set; }
        public int LocationId { get; set; }
        public String LocationCode { get; set; }
        public int NumTimesToRun { get; set; }
        public TimeSpan StartTime { get; set; }
        public int LengthDays { get; set; }
        public int MaxPossibleIterations { get; set; }
        public string Reason { get; set; }
        public OptimizerInputPrintable(OptimizerInput input)
        {
            Id = input.Id;
            CourseId = input.CourseId;
            CourseCode = input.CourseCode;
            LocationId = input.LocationIdLiteral;
            LocationCode = input.LocationID;
            NumTimesToRun = input.NumTimesToRun;
            StartTime = input.StartTime;
            LengthDays = input.LengthDays;
            MaxPossibleIterations = input.MaxPossibleIterations;
            Reason = input.Reason;
        }
    }
}
