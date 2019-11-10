using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OptimizerEngine.Models
{
    public partial class DatabaseContext : DbContext
    {
        public DatabaseContext()
        {

        }
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Attendance> Attendance { get; set; }
        public virtual DbSet<Booking> Booking { get; set; }
        public virtual DbSet<BookingCategory> BookingCategory { get; set; }
        public virtual DbSet<BookingCode> BookingCode { get; set; }
        public virtual DbSet<BookingHistory> BookingHistory { get; set; }
        public virtual DbSet<ClassNumberOfSubject> ClassNumberOfSubject { get; set; }
        public virtual DbSet<ClassRoster> ClassRoster { get; set; }
        public virtual DbSet<Course> Course { get; set; }
        public virtual DbSet<CourseHistory> CourseHistory { get; set; }
        public virtual DbSet<CourseRequiredResources> CourseRequiredResources { get; set; }
        public virtual DbSet<InstructorOfClass> InstructorOfClass { get; set; }
        public virtual DbSet<InstructorStatus> InstructorStatus { get; set; }
        public virtual DbSet<Location> Location { get; set; }
        public virtual DbSet<OptimizerInput> OptimizerInput { get; set; }
        public virtual DbSet<OptimizerResult> OptimizerResult { get; set; }
        public virtual DbSet<Qualification> Qualification { get; set; }
        public virtual DbSet<RequestedCourse> RequestedCourse { get; set; }
        public virtual DbSet<Role> Role { get; set; }
        public virtual DbSet<Room> Room { get; set; }
        public virtual DbSet<RoomHasResources> RoomHasResources { get; set; }
        public virtual DbSet<ScheduledClass> ScheduledClass{get; set; }
        public virtual DbSet<Status> Status { get; set; }
        public virtual DbSet<User> User { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=DESKTOP-I5E3UDR;Database=Optimizer_LSS_Local;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasKey(e => new { e.ClassRosterId, e.SubjectNumber });

                entity.Property(e => e.ClassRosterId).HasColumnName("ClassRosterID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AdminComment).HasColumnType("text");

                entity.Property(e => e.CodeId).HasColumnName("CodeID");

                entity.Property(e => e.EndDate).HasColumnType("date");

                entity.Property(e => e.LastTouchedBy)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.LocationId).HasColumnName("LocationID");

                entity.Property(e => e.RequestById)
                    .IsRequired()
                    .HasColumnName("RequestByID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.RequestComment).HasColumnType("text");

                entity.Property(e => e.RequestForId)
                    .IsRequired()
                    .HasColumnName("RequestForID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");

                entity.Property(e => e.StartDate).HasColumnType("date");
            });

            modelBuilder.Entity<BookingCategory>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<BookingCode>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedNever();

                entity.Property(e => e.CategoryId).HasColumnName("CategoryID");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<BookingHistory>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.AdminComment).HasColumnType("text");

                entity.Property(e => e.BookingId).HasColumnName("BookingID");

                entity.Property(e => e.CodeId).HasColumnName("CodeID");

                entity.Property(e => e.EndDate).HasColumnType("date");

                entity.Property(e => e.LocationId).HasColumnName("LocationID");

                entity.Property(e => e.RequestById)
                    .IsRequired()
                    .HasColumnName("RequestByID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.RequestComment).HasColumnType("text");

                entity.Property(e => e.RequestForId)
                    .IsRequired()
                    .HasColumnName("RequestForID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ScheduleId).HasColumnName("ScheduleID");

                entity.Property(e => e.StartDate).HasColumnType("date");
            });

            modelBuilder.Entity<ClassNumberOfSubject>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ScheduledClassId).HasColumnName("ScheduledClassID");
            });

            modelBuilder.Entity<ClassRoster>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Comment).HasColumnType("text");

                entity.Property(e => e.FirstName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.LastName)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Point)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ScheduledClassId).HasColumnName("ScheduledClassID");

                entity.Property(e => e.StudentFileNumber)
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => new { e.Code, e.Id });

                entity.Property(e => e.Code)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Atachapter)
                    .HasColumnName("ATAChapter")
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Comments)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.SpecialReq)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<CourseHistory>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ActiveCourseId).HasColumnName("ActiveCourseID");

                entity.Property(e => e.Atachapter)
                    .HasColumnName("ATAChapter")
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Comments)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.SpecialReq)
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<InstructorOfClass>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ClassId).HasColumnName("ClassID");

                entity.Property(e => e.EndDate).HasColumnType("date");

                entity.Property(e => e.StartDate).HasColumnType("date");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasColumnName("UserID")
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<InstructorStatus>(entity =>
            {
                entity.HasKey(e => e.RecordId);

                entity.Property(e => e.RecordId).HasColumnName("RecordID");

                entity.Property(e => e.Comment).HasColumnType("text");

                entity.Property(e => e.CourseId).HasColumnName("CourseID");

                entity.Property(e => e.InputBy)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.InstructorId)
                    .IsRequired()
                    .HasColumnName("InstructorID")
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<OptimizerInput>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CourseCode)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.CourseTitle)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.LocationId)
                    .IsRequired()
                    .HasColumnName("LocationID")
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.Reason).HasMaxLength(100);
            });

            modelBuilder.Entity<OptimizerResult>(entity =>
            {
                entity.Property(e => e.ID).HasColumnName("ID");
            });

            modelBuilder.Entity<Qualification>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false);

                entity.Property(e => e.QualificationCode)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false);

                entity.Property(e => e.Week1).HasColumnName("Week_1");

                entity.Property(e => e.Week2).HasColumnName("Week_2");

                entity.Property(e => e.Week3).HasColumnName("Week_3");

                entity.Property(e => e.Week4).HasColumnName("Week_4");
            });

            modelBuilder.Entity<RequestedCourse>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Comment)
                    .IsRequired()
                    .HasColumnType("text");

                entity.Property(e => e.CourseId).HasColumnName("CourseID");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.LocationId).HasColumnName("LocationID");

                entity.Property(e => e.NeededBy).HasColumnType("date");

                entity.Property(e => e.Phone)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Reason)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.RequestById)
                    .IsRequired()
                    .HasColumnName("RequestByID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.ResponseComment).HasColumnType("text");

                entity.Property(e => e.Shift)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.TimeStamp).HasColumnType("datetime");

                entity.Property(e => e.TimelineReason).HasColumnType("text");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedNever();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Location)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Notes)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.Property(e => e.Number)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Owner)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.ProjectionNotes)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.RoomType)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.Station)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<ScheduledClass>(entity =>
            {
                entity.Property(e => e.ID).HasColumnName("ID");
            });

            modelBuilder.Entity<Status>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.FileNumber);

                entity.Property(e => e.FileNumber)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.MiddleInitial)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.PointId).HasColumnName("PointID");

                entity.Property(e => e.RoleId).HasColumnName("RoleID");

                entity.Property(e => e.SupervisorId)
                    .HasColumnName("SupervisorID")
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);
            });
        }
    }
}
