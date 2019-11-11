using ConsoleTables;
using LSS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LSS.Services
{
    class OptimizerEngineBuilder
    {
        private OptimizerEngine Engine;
        private bool ShowDebugMessages;
        private DatabaseContext context;

        public OptimizerEngineBuilder(DatabaseContext _context, bool debug = false)
        {
            ShowDebugMessages = debug;
            Engine = new OptimizerEngine(_context);
            context = _context;
        }

        /// <summary>
        /// Populates the properties of the optimizer object by pulling in data from the database context
        /// </summary>
        /// <param name="showSetup">Will write to console debug images to display the data that is pulled in</param>
        /// <param name="StartDate">The beginning of the optimization range</param>
        /// <param name="EndDate">The end of the optimizaiton range</param>
        internal OptimizerEngine Build(DateTime Start, DateTime End)
        {
            if (ShowDebugMessages) Console.WriteLine("Pulling in data from the database...\n");
            Engine.StartDate = Start;
            Engine.EndDate = End;

            // Pull in instructors
            Engine.Instructors = context.User.Where(user => user.RoleID == 3).ToList();

            // Course Catalog
            Engine.CourseCatalog = context.Course.ToList();

            // Add to each course the instructors qualified to teach them
            context.InstructorStatus.
                Where(status => status.Deleted == false).ToList().
                ForEach(status => Engine.CourseCatalog.
                Where(course => course.ID == status.CourseID)
                .FirstOrDefault().
                QualifiedInstructors.Add(status.InstructorID));

            // Add to each course the required resources it needs from a room
            context.CourseRequiredResources.ToList().
                ForEach(required => Engine.CourseCatalog.Where(course => course.ID == required.CourseID).
                FirstOrDefault().RequiredResources[required.ResourceID] = required.Amount);

            // Rooms
            Engine.Rooms = context.Room.ToDictionary(room => room.ID, room => room);
            // Add to each room the resources it has
            context.RoomHasResources.ToList().ForEach(resources => Engine.Rooms.
            Where(room => room.Key == resources.RoomID).FirstOrDefault().Value.
            Resources_dict[resources.ID] = resources.Amount);

            // Get the optimizer input data
            Engine.Inputs = (from input in context.OptimizerInput
                        join course in context.Course
                        on input.CourseCode equals course.Code
                        join location in context.Location
                        on input.LocationID equals location.Code
                        where input.Succeeded == false
                        orderby course.Hours descending
                        select new OptimizerInput()
                        {
                            Id = input.Id,
                            CourseCode = input.CourseCode,
                            LocationID = input.LocationID,
                            NumTimesToRun = input.NumTimesToRun,
                            StartTime = input.StartTime,
                            CourseTitle = input.CourseTitle,
                            Succeeded = input.Succeeded,
                            Reason = input.Reason,
                            LengthDays = Math.Max(course.Hours / 8, 1),
                            CourseId = (int)course.ID,
                            LocationIdLiteral = location.ID
                        }).ToList<OptimizerInput>();
            if (ShowDebugMessages)
            {
                Console.WriteLine("Optimizer Inputs");
                ConsoleTable.From<OptimizerInput>(Engine.Inputs).Write(Format.MarkDown);
                Console.WriteLine("");
            }

            // Scheduled/Planned classes
            // Only grab classes within the optimzer time range and that aren't cancelled
            Engine.CurrentSchedule = context.ScheduledClass
                .Where(c => c.Cancelled == false && (
                (DateTime.Compare(c.StartDate, Engine.StartDate) >= 0 && DateTime.Compare(c.StartDate, Engine.EndDate) <= 0) ||
                (DateTime.Compare(c.EndDate, Engine.StartDate) >= 0 && DateTime.Compare(c.EndDate, Engine.EndDate) <= 0) ||
                (DateTime.Compare(c.StartDate, Engine.StartDate) < 0 && DateTime.Compare(c.EndDate, Engine.EndDate) > 0)
                )).ToList();
            if (ShowDebugMessages)
            {
                Console.WriteLine("Current Schedule:");
                ConsoleTable.From<ScheduledClassPrintable>(Engine.CurrentSchedule.Select(c => new ScheduledClassPrintable(c))).Write(Format.MarkDown);
                Console.WriteLine("");
            }

            // Instructor assignments
            Engine.InstructorAssignments = context.InstructorOfClass
                    .Where(i => i.Cancelled == false && (
                    (DateTime.Compare(i.StartDate, Engine.StartDate) >= 0 && DateTime.Compare(i.StartDate, Engine.EndDate) <= 0) ||
                    (DateTime.Compare(i.EndDate, Engine.StartDate) >= 0 && DateTime.Compare(i.EndDate, Engine.EndDate) <= 0) ||
                    (DateTime.Compare(i.StartDate, Engine.StartDate) < 0 && DateTime.Compare(i.EndDate, Engine.EndDate) > 0)
                    )).ToList();
            // Remove instructor assignments if the instructor is not in the user table
            Engine.InstructorAssignments.RemoveAll(assignment => Engine.Instructors.Any(instructor => instructor.Username == assignment.UserID));

            // Update instructor assignments by marking them if they are local assignments
            Engine.InstructorAssignments.
                ForEach(Assignment => Assignment.LocalAssignment = (
                Engine.Instructors.Where(instructor => instructor.Username == Assignment.UserID).First().PointID ==
                Engine.CurrentSchedule.Where(Class => Assignment.ClassID == Class.ID).First().LocationID));
            if (ShowDebugMessages)
            {
                Console.WriteLine("Instructor Assignments");
                ConsoleTable.From<InstructorOfClass>(Engine.InstructorAssignments).Write(Format.MarkDown);
                Console.WriteLine("");
            }

            // Exceptions
            Engine.Exceptions = context.Booking
                .Where(b => b.Status == 1 && b.Cancelled == false && (
                (DateTime.Compare(b.StartDate, Engine.StartDate) >= 0 && DateTime.Compare(b.StartDate, Engine.EndDate) <= 0) ||
                (DateTime.Compare(b.EndDate, Engine.StartDate) >= 0 && DateTime.Compare(b.EndDate, Engine.EndDate) <= 0) ||
                (DateTime.Compare(b.StartDate, Engine.StartDate) < 0 && DateTime.Compare(b.EndDate, Engine.EndDate) > 0))
                ).ToList();
            if (ShowDebugMessages)
            {
                Console.WriteLine("Bookings");
                ConsoleTable.From<BookingPrintable>(Engine.Exceptions.Select(e => new BookingPrintable(e))).Write(Format.MarkDown);
                Console.WriteLine("");
            }

            // Locations
            Engine.Locations = context.Location.ToList();
            // Populate the local rooms for each location
            context.Room.ToList().ForEach(room => Engine.Locations.Find(location => location.Code.Equals(room.Station)).LocalRooms.Add(room.ID));
            // Populate the local instructs for each location
            Engine.Instructors.ForEach(instructor => Engine.Locations.Find(location => location.ID == instructor.PointID).LocalInstructors.Add(instructor.Username));
            if (ShowDebugMessages)
            {
                Console.WriteLine("Locations");
                ConsoleTable.From<Location>(Engine.Locations).Write(Format.MarkDown);
                Console.WriteLine("");

                Console.WriteLine("Locations' Local Rooms");
                OptimizerUtilities.PrintRoomTable(Engine.Locations);
                Console.WriteLine("");

                Console.WriteLine("Locations' Local Instructors");
                OptimizerUtilities.PrintInstructorTable(Engine.Locations);
                Console.WriteLine("");
            }

            // Map each weekday of the range to an int reperesenting the index within the matrix
            // Key format => mm/dd/yyyy -> 3
            Engine.DateIndexMap = new Dictionary<string, int>();
            int x = 0;
            foreach (DateTime day in OptimizerUtilities.EachWeekDay(Engine.StartDate, Engine.EndDate))
                Engine.DateIndexMap[day.ToString(Engine.TIME_FORMAT)] = x++;
            if (ShowDebugMessages) Console.WriteLine("Count of week days in range: " + Engine.DateIndexMap.Count);

            // Create an index map for the number of rooms
            // will map the room id to the index in the matrix
            Engine.RoomIndexMap = new Dictionary<int, int>();
            x = 0;
            context.Room.ToList().ForEach(room => Engine.RoomIndexMap[room.ID] = x++);
            if (ShowDebugMessages) Console.WriteLine("Room count: " + Engine.RoomIndexMap.Count);

            // Create an index map for the number of instructors
            // will map the instructor username to the index in the matrix
            Engine.InstructorIndexMap = new Dictionary<string, int>();
            x = 0;
            Engine.Instructors.ForEach(instr => Engine.InstructorIndexMap[instr.Username] = x++);
            if (ShowDebugMessages) Console.WriteLine("Instructor count: " + Engine.InstructorIndexMap.Count);

            // Create an index map for the location to the matrix
            Engine.LocationIndexMap = new Dictionary<int, int>();
            x = 0;
            Engine.Locations.ForEach(loc => Engine.LocationIndexMap[loc.ID] = x++);

            // Create room unavailability matrix, i.e. true means rooms in unavailable
            Engine.IsRoomUnavailable = new bool[Engine.RoomIndexMap.Count, Engine.DateIndexMap.Count];

            // Create release count matrix
            Engine.CurrentlyReleased = new int[Engine.Locations.Count, Engine.DateIndexMap.Count];

            // Loop through the current schedule and populate the room availbility and the currently release dictionaries
            foreach (var clas in Engine.CurrentSchedule)
            {
                foreach (var date in OptimizerUtilities.EachWeekDay(OptimizerUtilities.Max(clas.StartDate, Engine.StartDate), OptimizerUtilities.Min(clas.EndDate, Engine.EndDate)))
                {
                    Engine.IsRoomUnavailable[Engine.RoomIndexMap[clas.RoomID], Engine.DateIndexMap[date.ToString(Engine.TIME_FORMAT)]] = true;
                    Engine.CurrentlyReleased[Engine.LocationIndexMap[clas.LocationID], Engine.DateIndexMap[date.ToString(Engine.TIME_FORMAT)]]
                        += Engine.CourseCatalog.Find(course => course.ID == clas.CourseID).MaxSize;
                }
            }
            if (ShowDebugMessages)
            {
                Console.WriteLine("Room Unavailability");
                OptimizerUtilities.PrintDate2DArray<bool, int>(Engine.IsRoomUnavailable, Engine.StartDate, Engine.EndDate, Engine.RoomIndexMap.Keys.ToList());
                Console.WriteLine("Current Released");
                OptimizerUtilities.PrintDate2DArray<int, int>(Engine.CurrentlyReleased, Engine.StartDate, Engine.EndDate, Engine.LocationIndexMap.Keys.ToList());
            }

            // Create instructor availability dictionary
            Engine.IsInstructorUnavailable = new bool[Engine.InstructorIndexMap.Count, Engine.DateIndexMap.Count];

            // Populate the unavailability area with the instructor assignments
            foreach (var assignment in Engine.InstructorAssignments)
            {
                // Mark each day of the relevant days of the assignment to the optimization range to true (unavailable)
                foreach (var date in OptimizerUtilities.EachWeekDay(OptimizerUtilities.Max(assignment.StartDate, Engine.StartDate), OptimizerUtilities.Min(assignment.EndDate, Engine.EndDate)))
                {
                    Engine.IsInstructorUnavailable[Engine.InstructorIndexMap[assignment.UserID], Engine.DateIndexMap[date.ToString(Engine.TIME_FORMAT)]] = true;
                }
                // Account for travel time by marking the day before and after for non local assignments unavailable for that instructor
                // Only if the day is adjacent to the assignment (i.e. not the friday before a monday of an assignment)
                if (!assignment.LocalAssignment)
                {
                    // Check if the day before the start day for the assignment is in the index map
                    // If its not, it is either an adjacent week day but outside the range of the optimizer
                    // Or it is a weekend (the day before monday is sunday, will not be mapped in the dictionary
                    var DayBeforeAssignment = OptimizerUtilities.Max(assignment.StartDate, Engine.StartDate).AddDays(-1).ToString(Engine.TIME_FORMAT);
                    if (Engine.DateIndexMap.ContainsKey(DayBeforeAssignment))
                    {
                        Engine.IsInstructorUnavailable[Engine.InstructorIndexMap[assignment.UserID], Engine.DateIndexMap[DayBeforeAssignment]] = true;
                    }
                    // Do same but for the day after
                    var DayAfterAssignment = OptimizerUtilities.Min(assignment.EndDate, Engine.EndDate).ToString(Engine.TIME_FORMAT);
                    if (Engine.DateIndexMap.ContainsKey(DayAfterAssignment))
                    {
                        Engine.IsInstructorUnavailable[Engine.InstructorIndexMap[assignment.UserID], Engine.DateIndexMap[DayAfterAssignment]] = true;
                    }
                }
            }

            // Add instructor exceptions to the unavailable dictionary 
            foreach (var exception in Engine.Exceptions)
            {
                foreach (var date in OptimizerUtilities.EachWeekDay(OptimizerUtilities.Max(exception.StartDate, Engine.StartDate), OptimizerUtilities.Min(exception.EndDate, Engine.EndDate)))
                {
                    if (Engine.InstructorIndexMap.ContainsKey(exception.RequestForID))
                    {
                        Engine.IsInstructorUnavailable[Engine.InstructorIndexMap[exception.RequestForID], Engine.DateIndexMap[date.ToString(Engine.TIME_FORMAT)]] = true;
                    }
                }
            }
            if (ShowDebugMessages)
            {
                Console.WriteLine("Instructor Unavilability");
                OptimizerUtilities.PrintDate2DArray<bool, string>(Engine.IsInstructorUnavailable, Engine.StartDate, Engine.EndDate, Engine.InstructorIndexMap.Keys.ToList());
            }
            
            if (ShowDebugMessages) Console.WriteLine("Data loading complete.\n");

            return Engine;
        }
    }
}
