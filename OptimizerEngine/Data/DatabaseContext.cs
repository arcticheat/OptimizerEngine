using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LSS.Models;

namespace LSS.Models
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext()
        {

        }
        //public DatabaseContext(DbContextOptions<DatabaseContext> options)
        //    : base(options)
        //{
        //}

        public DbSet<LSS.Models.Attendance> Attendance { get; set; }

        public DbSet<LSS.Models.ClassNumberOfSubject> ClassNumberOfSubject { get; set; }

        public DbSet<LSS.Models.Booking> Booking { get; set; }

        public DbSet<LSS.Models.User> User { get; set; }

        public DbSet<LSS.Models.BookingCode> BookingCode { get; set; }

        public DbSet<LSS.Models.BookingCategory> BookingCategory { get; set; }

        public DbSet<LSS.Models.Course> Course { get; set; }

        public DbSet<LSS.Models.Location> Location { get; set; }

        public DbSet<LSS.Models.ScheduledClass> ScheduledClass { get; set; }

        public DbSet<LSS.Models.Status> Status { get; set; }

        public DbSet<LSS.Models.Room> Room { get; set; }

        public DbSet<LSS.Models.InstructorOfClass> InstructorOfClass { get; set; }

        public DbSet<LSS.Models.CourseCategories> CourseCategories { get; set; }

        public DbSet<LSS.Models.CatalogCodes> CatalogCodes { get; set; }

        public DbSet<LSS.Models.InstructorStatus> InstructorStatus { get; set; }

        public DbSet<LSS.Models.Qualification> Qualification { get; set; }

        public DbSet<LSS.Models.BookingHistory> BookingHistory { get; set; }

        public DbSet<LSS.Models.RequestedCourse> RequestedCourse { get; set; }

        public DbSet<LSS.Models.InstructorOfClassHistory> InstructorOfClassHistory { get; set; }

        public DbSet<LSS.Models.ScheduledClassHistory> ScheduledClassHistory { get; set; }

        public DbSet<LSS.Models.ClassRoster> ClassRoster { get; set; }

        public DbSet<LSS.Models.RoomHasResources> RoomHasResources { get; set; }

        public DbSet<LSS.Models.RoomResources> RoomResources { get; set; }

        public DbSet<LSS.Models.CourseRequiredResources> CourseRequiredResources { get; set; }

        public DbSet<LSS.Models.Role> Role { get; set; }

        public DbSet<LSS.Models.Notify> Notify { get; set; }

        public DbSet<LSS.Models.CancellationCodes> CancellationCodes { get; set; }

        public DbSet<LSS.Models.OptimizerInput> OptimizerInput { get; set; }

        public DbSet<LSS.Models.OptimizerResult> OptimizerResult { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=DESKTOP-I5E3UDR;Initial Catalog=Optimizer_LSS_Local;Integrated Security=true;");
            }
        }
    }
}
