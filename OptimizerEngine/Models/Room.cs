using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptimizerEngine.Models
{
    public partial class Room
    {
        public int Id { get; set; }
        public string Station { get; set; }
        public string Number { get; set; }
        public string Location { get; set; }
        public string Owner { get; set; }
        public string RoomType { get; set; }
        public string ProjectionNotes { get; set; }
        public bool Active { get; set; }
        public string Notes { get; set; }
        [NotMapped]
        public Dictionary<int, int?> Resources = new Dictionary<int, int?>();
    }
}
