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
        private DateTime StartDate;
        private DateTime EndDate;
        public bool ShowDebugMessages;
        public Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable = new Dictionary<int, Dictionary<string, bool>>();
        public Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable = new Dictionary<string, Dictionary<string, bool>>();
        public Dictionary<int, Dictionary<string, int>> CurrentlyReleased = new Dictionary<int, Dictionary<string, int>>();
        public Dictionary<int, Dictionary<string, List<int>>> LocallyTaughtCoursesPerDay = new Dictionary<int, Dictionary<string, List<int>>>();
        public string TIME_FORMAT = "MM/dd/yyyy";


        public OptimizerEngineBuilder(DateTime start, DateTime end, bool debug = false)
        {
            ShowDebugMessages = debug;
            StartDate = start;
            EndDate = end;
            Engine = new OptimizerEngine();
        }

        /// <summary>
        /// Populates the properties of the optimizer object by pulling in data from the database context
        /// </summary>
        /// <param name="showSetup">Will write to console debug images to display the data that is pulled in</param>
        /// <param name="StartDate">The beginning of the optimization range</param>
        /// <param name="EndDate">The end of the optimizaiton range</param>
        internal OptimizerEngine Build()
        {
            if (ShowDebugMessages) Console.WriteLine("Pulling in data from the database...\n");
            Engine.ShowDebugMessages = ShowDebugMessages;
            Engine.StartDate = StartDate;
            Engine.EndDate = EndDate;
            using (var context = new DatabaseContext())
            {
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
                              LocationIdLiteral = location.ID,
                              RemainingRuns = input.NumTimesToRun
                          }).ToList<OptimizerInput>();
                Engine.InputCount = Engine.Inputs.Count;
                var Levels = 0;
                Engine.Inputs.ForEach(x => Levels += x.NumTimesToRun);
                Engine.NodesPerDepth = new int[Levels];
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
                    Console.WriteLine("Current Schedule");
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
                    ConsoleTable.From(Engine.Locations).Write(Format.MarkDown);
                    Console.WriteLine("");


                    Console.WriteLine("Locations' Local Rooms");
                    OptimizerUtilities.PrintRoomTable(Engine.Locations);
                    Console.WriteLine("");

                    Console.WriteLine("Locations' Local Instructors");
                    OptimizerUtilities.PrintInstructorTable(Engine.Locations);
                    Console.WriteLine("");
                }

                // Store all the week days of the range into a list
                var WeekDaysInRange = new List<string>();

                foreach (DateTime day in OptimizerUtilities.EachWeekDay(Engine.StartDate, Engine.EndDate))
                {
                    WeekDaysInRange.Add(day.ToString(TIME_FORMAT));
                }
                if (ShowDebugMessages) Console.WriteLine("Count of week days in range: " + WeekDaysInRange.Count());
                Engine.TotalWeekDays = WeekDaysInRange.Count;

                // Link each location to its locally taught courses per day
                foreach (var loc in Engine.Locations)
                {
                    LocallyTaughtCoursesPerDay.Add(loc.ID, WeekDaysInRange.ToDictionary(x => x, x => new List<int>()));
                }


                // Fill the 2d dictionary IsRoomUnavailable
                // First pair: value is the room id to a second dictionary
                // Second pair: string of the date to the unavailability of the room
                context.Room.ToList().ForEach(room => IsRoomUnavailable.Add(room.ID, WeekDaysInRange.ToDictionary(x => x, x => false)));

                // Fill the 2d dictionary IsInstructorUnavailable
                // First pair: value is the instructor username to a second dictionary
                // Second pair: string of the date to the unavailability of the instructor
                Engine.Instructors.ForEach(instructor => IsInstructorUnavailable.Add(instructor.Username, WeekDaysInRange.ToDictionary(x => x, x=> false)));

                // Fill the 2d dictionary CurrentlyReleased
                // First pair: value is the location id to a second dictionary
                // Second pair: string of the date to the unavailability of the instructor
                Engine.Locations.ForEach(loc => CurrentlyReleased.Add(loc.ID, WeekDaysInRange.ToDictionary(x => x, x=> 0)));

                // Loop through the current schedule and populate the room availbility and the currently release dictionaries
                foreach (var clas in Engine.CurrentSchedule)
                {
                    foreach (var date in OptimizerUtilities.EachWeekDay(OptimizerUtilities.Max(clas.StartDate, Engine.StartDate), OptimizerUtilities.Min(clas.EndDate, Engine.EndDate)))
                    {
                        IsRoomUnavailable[clas.RoomID][date.ToString(TIME_FORMAT)] = true;
                        CurrentlyReleased[clas.LocationID][date.ToString(TIME_FORMAT)] += Engine.CourseCatalog.Find(course => course.ID == clas.CourseID).MaxSize;
                        LocallyTaughtCoursesPerDay[clas.LocationID][date.ToString(TIME_FORMAT)].Add(clas.CourseID);
                    }
                }
                if (ShowDebugMessages)
                {
                    Console.WriteLine("IsRoomUnavailable");
                    var Headers = new List<string> { "RoomID" };
                    WeekDaysInRange.ForEach(day => Headers.Add(day.Substring(0, day.Length - 5)));
                    var table = new ConsoleTable(Headers.ToArray());
                    foreach (var RoomPair in IsRoomUnavailable)
                    {
                        var row = new List<string> { RoomPair.Key.ToString() };
                        RoomPair.Value.ToList().ForEach(pair => row.Add(pair.Value.ToString()));
                        table.AddRow(row.ToArray());
                    }
                    table.Write(Format.MarkDown);

                    Console.WriteLine("CurrentlyReleased");
                    Headers = new List<string> { "LocationID" };
                    WeekDaysInRange.ForEach(day => Headers.Add(day.Substring(0, day.Length - 5)));
                    table = new ConsoleTable(Headers.ToArray());
                    foreach (var LocPair in CurrentlyReleased)
                    {
                        var row = new List<string> { LocPair.Key.ToString() };
                        LocPair.Value.ToList().ForEach(pair => row.Add(pair.Value.ToString()));
                        table.AddRow(row.ToArray());
                    }
                    table.Write(Format.MarkDown);

                    Console.WriteLine("Locally taught courses at each location");
                    Headers = new List<string> { "LocationID" };
                    WeekDaysInRange.ForEach(day => Headers.Add(day.Substring(0, day.Length - 5)));
                    table = new ConsoleTable(Headers.ToArray());
                    foreach (var Location in LocallyTaughtCoursesPerDay)
                    {
                        var row = new List<string> { Location.Key.ToString() };
                        Location.Value.Values.ToList().ForEach(list => row.Add(string.Join(", ", list)));
                        table.AddRow(row.ToArray());
                    }
                    table.Write(Format.MarkDown);
                }

                // Populate the unavailability area with the instructor assignments
                foreach (var assignment in Engine.InstructorAssignments)
                {
                    // Mark each day of the relevant days of the assignment to the optimization range to true (unavailable)
                    foreach (var date in OptimizerUtilities.EachWeekDay(OptimizerUtilities.Max(assignment.StartDate, Engine.StartDate), OptimizerUtilities.Min(assignment.EndDate, Engine.EndDate)))
                    {
                        IsInstructorUnavailable[assignment.UserID][date.ToString(TIME_FORMAT)] = true;
                    }
                    // Account for travel time by marking the day before and after for non local assignments unavailable for that instructor
                    // Only if the day is adjacent to the assignment (i.e. not the friday before a monday of an assignment)
                    if (!assignment.LocalAssignment)
                    {
                        // Check if the day before the start day for the assignment is in the index map
                        // If its not, it is either an adjacent week day but outside the range of the optimizer
                        // Or it is a weekend (the day before monday is sunday, will not be mapped in the dictionary
                        var DayBeforeAssignment = OptimizerUtilities.Max(assignment.StartDate, Engine.StartDate).AddDays(-1).ToString(Engine.TIME_FORMAT);
                        if (WeekDaysInRange.Contains(DayBeforeAssignment))
                        {
                            IsInstructorUnavailable[assignment.UserID][DayBeforeAssignment] = true;
                        }
                        // Do same but for the day after
                        var DayAfterAssignment = OptimizerUtilities.Min(assignment.EndDate, Engine.EndDate).ToString(Engine.TIME_FORMAT);
                        if (WeekDaysInRange.Contains(DayAfterAssignment))
                        {
                            IsInstructorUnavailable[assignment.UserID][DayAfterAssignment] = true;
                        }
                    }
                }

                // Add instructor exceptions to the unavailable dictionary 
                foreach (var exception in Engine.Exceptions)
                {
                    foreach (var date in OptimizerUtilities.EachWeekDay(OptimizerUtilities.Max(exception.StartDate, Engine.StartDate), OptimizerUtilities.Min(exception.EndDate, Engine.EndDate)))
                    {
                        if (WeekDaysInRange.Contains(exception.RequestForID))
                        {
                            //Engine.IsInstructorUnavailable[Engine.InstructorIndexMap[exception.RequestForId], Engine.DateIndexMap[date.ToString(Engine.TIME_FORMAT)]] = true;
                            IsInstructorUnavailable[exception.RequestForID][date.ToString(TIME_FORMAT)] = true;
                        }
                    }
                }
                if (ShowDebugMessages)
                {
                    Console.WriteLine("IsInstructorUnavailable");
                    var Headers = new List<string> { "Username" };
                    WeekDaysInRange.ForEach(day => Headers.Add(day.Substring(0, day.Length - 5)));
                    var table = new ConsoleTable(Headers.ToArray());
                    foreach (var InstructorPair in IsInstructorUnavailable)
                    {
                        var row = new List<string> { InstructorPair.Key.ToString() };
                        InstructorPair.Value.ToList().ForEach(pair => row.Add(pair.Value.ToString()));
                        table.AddRow(row.ToArray());
                    }
                    table.Write(Format.MarkDown);
                }
            }

            // Setup the best answer as something guarenteed to always be the worst
            Engine.CurrentBestAnswer = new OptimizerScheduleResults
            {
                Results = new List<OptimizerResult>(),
                Inputs = new List<OptimizerInput>(),
                OptimizationScore = -1
            };

            if (ShowDebugMessages) Console.WriteLine("Data loading complete.\n");

            return Engine;
        }
    }
}
