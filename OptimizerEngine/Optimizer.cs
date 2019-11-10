using ConsoleTables;
using OptimizerEngine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OptimizerEngine
{
    class Optimizer
    {
        private List<OptimizerInput> Inputs;
        private List<ScheduledClass> CurrentSchedule;
        private List<InstructorOfClass> InstructorAssignments;
        private List<User> Instructors;
        private List<Booking> Exceptions;
        private List<Location> Locations;
        private List<Course> CourseCatalog;
        private Dictionary<int, Room> Rooms;
        private Dictionary<String, int> DateIndexMap;
        private Dictionary<int, int> RoomIndexMap;
        private Dictionary<string, int> InstructorIndexMap;
        private Dictionary<int, int> LocationIndexMap;
        private bool[,] IsRoomUnavailable;
        private bool[,] IsInstructorUnavailable;
        private int[,] CurrentlyReleased;
        private string TIME_FORMAT = "MM/dd/yyyy";
        private DateTime StartDate;
        private DateTime EndDate;

        /* HELPER FUNCTIONS */
        public static DateTime Max(DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }

        public static DateTime Min(DateTime a, DateTime b)
        {
            return a < b ? a : b;
        }

        // source
        // https://stackoverflow.com/questions/1847580/how-do-i-loop-through-a-date-range
        public static IEnumerable<DateTime> EachWeekDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
            {
                if ((day.DayOfWeek != DayOfWeek.Saturday) && (day.DayOfWeek != DayOfWeek.Sunday))
                {
                    yield return day;
                }
            }
        }

        /// <summary>
        /// Returns the work day however many days after the start date 
        /// </summary>
        /// <param name="startDate">The original date</param>
        /// <param name="workdays">How many days to go backwards from</param>
        /// <param name="substract">Set true to subtract week days instead</param>
        /// <returns></returns>
        public static DateTime AddWeekDays(DateTime startDate, int workdays, bool subtract = false)
        {
            DateTime date = startDate;
            while (workdays > 0)
            {
                if (subtract)
                    date = date.AddDays(-1);
                else
                    date = date.AddDays(1);
                if (date.DayOfWeek < DayOfWeek.Saturday &&
                    date.DayOfWeek > DayOfWeek.Sunday)
                    workdays--;
            }
            return date;
        }

        public static void PrintDate2DArray<T1, T2>(T1[,] matrix, DateTime StartDate, DateTime EndDate, List<T2> RowNames)
        {
            Console.Write("|\t");
            foreach (DateTime day in EachWeekDay(StartDate, EndDate))
            {
                Console.Write("|" + day.Month + "-" + day.Day + "\t");
            }
            Console.WriteLine("|");
            Console.Write("|-------");
            foreach (DateTime day in EachWeekDay(StartDate, EndDate))
            {
                Console.Write("|-------");
            }
            Console.WriteLine("|");

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                Console.Write("|" + RowNames[i]);
                if (RowNames[i].ToString().Length < 7)
                    Console.Write("\t");
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    Console.Write("|" + matrix[i, j] + "\t");
                }
                Console.WriteLine("|");
            }
            Console.WriteLine("");
        }

        public static void PrintRoomTable(List<Location> Locations)
        {
            Console.WriteLine("| LocationID | Local Room IDs |");
            Console.WriteLine("|------------|----------------|");
            foreach (var l in Locations)
            {
                if (!l.HasLocalRooms()) continue;
                Console.Write($"| {l.Id}         ");
                if (l.Id / 10 == 0)
                    Console.Write(" ");
                Console.Write("|");
                string RoomsString = " ";
                foreach (var r in l.LocalRooms)
                {
                    RoomsString += r + ", ";
                }
                if (RoomsString.Length > 1)
                {
                    RoomsString = RoomsString.Substring(0, RoomsString.Length - 2);
                }
                Console.WriteLine(RoomsString);

            }
            Console.WriteLine("");
        }
        public static void PrintInstructorTable(List<Location> Locations)
        {
            Console.WriteLine("| LocationID | Local Instructor IDs |");
            Console.WriteLine("|------------|----------------------|");
            foreach (var l in Locations)
            {
                if (!l.HasLocalInstructors()) continue;
                Console.Write($"| {l.Id}         ");
                if (l.Id / 10 == 0)
                    Console.Write(" ");
                Console.Write("|");
                string InstructorString = " ";
                int InstructorsOnLine = 0;
                foreach (var i in l.LocalInstructors)
                {
                    if (InstructorsOnLine >= 10)
                    {
                        InstructorString += "\n|------------| ";
                        InstructorsOnLine = 0;
                    }
                    InstructorString += i + ", ";
                    InstructorsOnLine++;

                }
                if (InstructorString.Length > 1)
                {
                    InstructorString = InstructorString.Substring(0, InstructorString.Length - 2);
                }
                Console.WriteLine(InstructorString);

            }
            Console.WriteLine("");
        }
        /* END OF HELPER FUNCTIONS */

        /// <summary>
        /// Populates the properties of the optimizer object by pulling in data from the database context
        /// </summary>
        /// <param name="showSetup">Will write to console debug images to display the data that is pulled in</param>
        /// <param name="StartDate">The beginning of the optimization range</param>
        /// <param name="EndDate">The end of the optimizaiton range</param>
        internal void PullInData(bool showSetup, DateTime Start, DateTime End)
        {
            Console.WriteLine("Pulling in data from the database...\n");
            StartDate = Start;
            EndDate = End;
            using (var context = new DatabaseContext())
            {
                // Pull in instructors
                Instructors = context.User.Where(user => user.RoleId == 3).ToList();

                // Course Catalog
                CourseCatalog = context.Course.ToList();

                // Add to each course the instructors qualified to teach them
                context.InstructorStatus.
                    Where(status => status.Deleted == false).ToList().
                    ForEach(status => CourseCatalog.
                    Where(course => course.Id == status.CourseId)
                    .FirstOrDefault().
                    QualifiedInstructors.Add(status.InstructorId));

                // Add to each course the required resources it needs from a room
                context.CourseRequiredResources.ToList().
                    ForEach(required => CourseCatalog.Where(course => course.Id == required.CourseID).
                    FirstOrDefault().RequiredResources[required.ResourceID] = required.Amount);

                // Rooms
                Rooms = context.Room.ToDictionary(room => room.Id, room => room);
                // Add to each room the resources it has
                context.RoomHasResources.ToList().ForEach(resources => Rooms.
                Where(room => room.Key == resources.RoomID).FirstOrDefault().Value.
                Resources[resources.ID] = resources.Amount);

                // Get the optimizer input data
                Inputs = (from input in context.OptimizerInput
                          join course in context.Course
                            on input.CourseCode equals course.Code
                          join location in context.Location
                            on input.LocationId equals location.Code
                          where input.Succeeded == false
                          orderby course.Hours descending
                          select new OptimizerInput()
                          {
                              Id = input.Id,
                              CourseCode = input.CourseCode,
                              LocationId = input.LocationId,
                              NumTimesToRun = input.NumTimesToRun,
                              StartTime = input.StartTime,
                              CourseTitle = input.CourseTitle,
                              Succeeded = input.Succeeded,
                              Reason = input.Reason,
                              LengthDays = Math.Max(course.Hours / 8, 1),
                              CourseId = (int)course.Id,
                              LocationIdLiteral = location.Id
                          }).ToList<OptimizerInput>();
                if (showSetup)
                {
                    Console.WriteLine("Optimizer Inputs");
                    ConsoleTable.From<OptimizerInput>(Inputs).Write(Format.MarkDown);
                    Console.WriteLine("");
                }

                // Scheduled/Planned classes
                // Only grab classes within the optimzer time range and that aren't cancelled
                CurrentSchedule = context.ScheduledClass
                    .Where(c => c.Cancelled == false && (
                    (DateTime.Compare(c.StartDate, StartDate) >= 0 && DateTime.Compare(c.StartDate, EndDate) <= 0) ||
                    (DateTime.Compare(c.EndDate, StartDate) >= 0 && DateTime.Compare(c.EndDate, EndDate) <= 0) ||
                    (DateTime.Compare(c.StartDate, StartDate) < 0 && DateTime.Compare(c.EndDate, EndDate) > 0)
                    )).ToList();
                if (showSetup)
                {
                    Console.WriteLine("Current Schedule:");
                    ConsoleTable.From<ScheduledClassPrintable>(CurrentSchedule.Select(c => new ScheduledClassPrintable(c))).Write(Format.MarkDown);
                    Console.WriteLine("");
                }

                // Instructor assignments
                InstructorAssignments = context.InstructorOfClass
                     .Where(i => i.Cancelled == false && (
                     (DateTime.Compare(i.StartDate, StartDate) >= 0 && DateTime.Compare(i.StartDate, EndDate) <= 0) ||
                     (DateTime.Compare(i.EndDate, StartDate) >= 0 && DateTime.Compare(i.EndDate, EndDate) <= 0) ||
                     (DateTime.Compare(i.StartDate, StartDate) < 0 && DateTime.Compare(i.EndDate, EndDate) > 0)
                     )).ToList();
                // Update instructor assignments by marking them if they are local assignments
                InstructorAssignments.
                    ForEach(Assignment => Assignment.LocalAssignment = (
                    Instructors.Where(instructor => instructor.Username == Assignment.UserId).First().PointId ==
                    CurrentSchedule.Where(Class => Assignment.ClassId == Class.ID).First().LocationID));
                if (showSetup)
                {
                    Console.WriteLine("Instructor Assignments");
                    ConsoleTable.From<InstructorOfClass>(InstructorAssignments).Write(Format.MarkDown);
                    Console.WriteLine("");
                }

                // Exceptions
                Exceptions = context.Booking
                    .Where(b => b.Status == 1 && b.Cancelled == false && (
                    (DateTime.Compare(b.StartDate, StartDate) >= 0 && DateTime.Compare(b.StartDate, EndDate) <= 0) ||
                    (DateTime.Compare(b.EndDate, StartDate) >= 0 && DateTime.Compare(b.EndDate, EndDate) <= 0) ||
                    (DateTime.Compare(b.StartDate, StartDate) < 0 && DateTime.Compare(b.EndDate, EndDate) > 0))
                    ).ToList();
                if (showSetup)
                {
                    Console.WriteLine("Bookings");
                    ConsoleTable.From<BookingPrintable>(Exceptions.Select(e => new BookingPrintable(e))).Write(Format.MarkDown);
                    Console.WriteLine("");
                }

                // Locations
                Locations = context.Location.ToList();
                // Populate the local rooms for each location
                context.Room.ToList().ForEach(room => Locations.Find(location => location.Code.Equals(room.Station)).LocalRooms.Add(room.Id));
                // Populate the local instructs for each location
                Instructors.ForEach(instructor => Locations.Find(location => location.Id == instructor.PointId).LocalInstructors.Add(instructor.Username));
                if (showSetup)
                {
                    Console.WriteLine("Locations");
                    ConsoleTable.From<Location>(Locations).Write(Format.MarkDown);
                    Console.WriteLine("");

                    Console.WriteLine("Locations' Local Rooms");
                    PrintRoomTable(Locations);
                    Console.WriteLine("");

                    Console.WriteLine("Locations' Local Instructors");
                    PrintInstructorTable(Locations);
                    Console.WriteLine("");
                }

                // Map each weekday of the range to an int reperesenting the index within the matrix
                // Key format => mm/dd/yyyy -> 3
                DateIndexMap = new Dictionary<string, int>();
                int x = 0;
                foreach (DateTime day in EachWeekDay(StartDate, EndDate))
                    DateIndexMap[day.ToString(TIME_FORMAT)] = x++;
                if (showSetup) Console.WriteLine("Count of week days in range: " + DateIndexMap.Count);

                // Create an index map for the number of rooms
                // will map the room id to the index in the matrix
                RoomIndexMap = new Dictionary<int, int>();
                x = 0;
                context.Room.ToList().ForEach(room => RoomIndexMap[room.Id] = x++);
                if (showSetup) Console.WriteLine("Room count: " + RoomIndexMap.Count);

                // Create an index map for the number of instructors
                // will map the instructor username to the index in the matrix
                InstructorIndexMap = new Dictionary<string, int>();
                x = 0;
                Instructors.ForEach(instr => InstructorIndexMap[instr.Username] = x++);
                if (showSetup) Console.WriteLine("Instructor count: " + InstructorIndexMap.Count);

                // Create an index map for the location to the matrix
                LocationIndexMap = new Dictionary<int, int>();
                x = 0;
                Locations.ForEach(loc => LocationIndexMap[loc.Id] = x++);

                // Create room unavailability matrix, i.e. true means rooms in unavailable
                IsRoomUnavailable = new bool[RoomIndexMap.Count, DateIndexMap.Count];

                // Create release count matrix
                CurrentlyReleased = new int[Locations.Count, DateIndexMap.Count];

                // Loop through the current schedule and populate the room availbility and the currently release dictionaries
                foreach (var clas in CurrentSchedule)
                {
                    foreach (var date in EachWeekDay(Max(clas.StartDate, StartDate), Min(clas.EndDate, EndDate)))
                    {
                        IsRoomUnavailable[RoomIndexMap[clas.RoomID], DateIndexMap[date.ToString(TIME_FORMAT)]] = true;
                        CurrentlyReleased[LocationIndexMap[clas.LocationID], DateIndexMap[date.ToString(TIME_FORMAT)]] += CourseCatalog.Find(course => course.Id == clas.CourseID).MaxSize;
                    }
                }
                if (showSetup)
                {
                    Console.WriteLine("Room Unavailability");
                    PrintDate2DArray<bool, int>(IsRoomUnavailable, StartDate, EndDate, RoomIndexMap.Keys.ToList());
                    Console.WriteLine("Current Released");
                    PrintDate2DArray<int, int>(CurrentlyReleased, StartDate, EndDate, LocationIndexMap.Keys.ToList());
                }

                // Create instructor availability dictionary
                IsInstructorUnavailable = new bool[InstructorIndexMap.Count, DateIndexMap.Count];

                // Populate the unavailability area with the instructor assignments
                foreach (var assignment in InstructorAssignments)
                {
                    // Mark each day of the relevant days of the assignment to the optimization range to true (unavailable)
                    foreach (var date in EachWeekDay(Max(assignment.StartDate, StartDate), Min(assignment.EndDate, EndDate)))
                    {
                        IsInstructorUnavailable[InstructorIndexMap[assignment.UserId], DateIndexMap[date.ToString(TIME_FORMAT)]] = true;
                    }
                    // Account for travel time by marking the day before and after for non local assignments unavailable for that instructor
                    // Only if the day is adjacent to the assignment (i.e. not the friday before a monday of an assignment)
                    if (!assignment.LocalAssignment)
                    {
                        // Check if the day before the start day for the assignment is in the index map
                        // If its not, it is either an adjacent week day but outside the range of the optimizer
                        // Or it is a weekend (the day before monday is sunday, will not be mapped in the dictionary
                        var DayBeforeAssignment = Max(assignment.StartDate, StartDate).AddDays(-1).ToString(TIME_FORMAT);
                        if (DateIndexMap.ContainsKey(DayBeforeAssignment))
                        {
                            IsInstructorUnavailable[InstructorIndexMap[assignment.UserId], DateIndexMap[DayBeforeAssignment]] = true;
                        }
                        // Do same but for the day after
                        var DayAfterAssignment = Min(assignment.EndDate, EndDate).ToString(TIME_FORMAT);
                        if (DateIndexMap.ContainsKey(DayAfterAssignment))
                        {
                            IsInstructorUnavailable[InstructorIndexMap[assignment.UserId], DateIndexMap[DayAfterAssignment]] = true;
                        }
                    }
                }

                // Add instructor exceptions to the unavailable dictionary 
                foreach (var exception in Exceptions)
                {
                    foreach (var date in EachWeekDay(Max(exception.StartDate, StartDate), Min(exception.EndDate, EndDate)))
                    {
                        if (InstructorIndexMap.ContainsKey(exception.RequestForId))
                        {
                            IsInstructorUnavailable[InstructorIndexMap[exception.RequestForId], DateIndexMap[date.ToString(TIME_FORMAT)]] = true;
                        }
                    }
                }
                if (showSetup)
                {
                    Console.WriteLine("Instructor Unavilability");
                    PrintDate2DArray<bool, string>(IsInstructorUnavailable, StartDate, EndDate, InstructorIndexMap.Keys.ToList());
                }
            }
            Console.WriteLine("Data loading complete.\n");
        }

        /// <summary>
        /// The optimizer will calculate a single collection of optimizer results created from the optimizer input
        /// The results are created by ensuring the schedule is legal as well as assigning the resources on a first available focus
        /// </summary>
        internal void OptimizeGreedy()
        {
            Console.WriteLine("Optimizing...\n");
            // Container for the results
            var Results = new List<OptimizerResult>();

            // Container for inputs that were unable to be scheduled
            var FailedToSchedule = new List<OptimizerInput>();

            // Loop through each input from the optimizer
            foreach (var CurrentInput in Inputs)
            {
                Console.WriteLine($"Calculating result for input ID {CurrentInput.Id}... ");

                // result object for this input
                var Result = new OptimizerResult
                {
                    InstrUsername = "",
                    RoomID = 0
                };

                // Obtain the class max size and the location release rate for the function call to find the valid start dates
                var MaxClassSize = CourseCatalog.Where(course => course.Id == CurrentInput.CourseId).First().MaxSize;
                var ReleaseRate = Locations.Where(location => location.Id == CurrentInput.LocationIdLiteral).First().ReleaseRate;

                // Obtain the information about this course in the catalog
                var CourseInfo = CourseCatalog.Where(course => course.Id == CurrentInput.CourseId).First();

                // Obtain all possible start dates (restricted by location release rate and course length)
                var ValidStartDates = FindValidStartDates(CurrentInput.LocationIdLiteral, CurrentInput.LengthDays, MaxClassSize, ReleaseRate);
                if (ValidStartDates.Count <= 0)
                {
                    Console.WriteLine($"The input could not be scheduled because the location {CurrentInput.LocationId} would exceed its release rate.\n");
                    CurrentInput.Reason = "Release rate would be exceeded.";
                    FailedToSchedule.Add(CurrentInput);
                }

                // Counter to keep track of inputs that need multiple iterations scheduled 
                var currentIterationForThisInput = 0;

                // Loop through each day within the optimizer range that the course could start on
                var lastDate = ValidStartDates.LastOrDefault();
                
                foreach (var ValidStartDate in ValidStartDates)
                {
                    Console.WriteLine($"Searching on the start date {ValidStartDate.ToString(TIME_FORMAT)} for an instructor and room.");
                    // Set the end date for this range based off of the course length
                    var ValidEndDate = ValidStartDate.AddDays(CurrentInput.LengthDays - 1);

                    // Loop through all qualified instructors for this course
                    // TODO: Check instructors in order of distance to the location
                    foreach (var Instructor in CourseInfo.QualifiedInstructors)
                    {
                        // Determine if this instructor is available for the range
                        if (IsInstructorAvailableForDateRange(Instructor, ValidStartDate, ValidEndDate))
                        {
                            Console.WriteLine($"The instructor {Instructor} is available.");
                            Result.InstrUsername = Instructor;
                            break;
                        }
                    }
                    // If no instructor is available, consider the next start date
                    if (Result.InstrUsername == "")
                    {
                        Console.WriteLine("No instructors are available. Moving on to the next valid start date.");
                        if (ValidStartDate == lastDate)
                        {
                            Console.WriteLine($"The input could not be scheduled because no instructor is available.\n");
                            CurrentInput.Reason = "No instructor is available.";
                            FailedToSchedule.Add(CurrentInput);
                        }
                        continue;
                    }
                    // Loop through all local rooms for this location
                    // but only the rooms that have the right type and quantity of resources required by this course type
                    foreach (var RoomID in Locations.Where(Loc => Loc.Code == CurrentInput.LocationId).First().
                        LocalRooms.Where(room => CourseInfo.RequiredResources.All(required =>
                        Rooms[room].Resources.ContainsKey(required.Key) && Rooms[room].Resources[required.Key] >= required.Value )))
                    {
                        // Determine if this room is available 
                        if (IsRoomAvailbleForDateRange(RoomID, ValidStartDate, ValidEndDate))
                        {
                            Console.WriteLine($"The Room {RoomID} is available.");
                            Result.RoomID = RoomID;
                            break;
                        }
                    }
                    // If no room is available, consider the next start date
                    if (Result.RoomID == 0)
                    {
                        Console.WriteLine("No room is available. Moving on to the next valid start date.");
                        Result.InstrUsername = "";
                        if (ValidStartDate == lastDate)
                        {
                            Console.WriteLine($"The above input could not be scheduled because no room is available.\n");
                            CurrentInput.Reason = "No local room is available.";
                            FailedToSchedule.Add(CurrentInput);
                        }
                        continue;
                    }
                    // If successful, update the matrices 
                    UpdateMatrices(ValidStartDate, ValidEndDate, CurrentInput.LocationIdLiteral, MaxClassSize, Result.InstrUsername, Result.RoomID);

                    // Found an answer so set the remaining fields for the result
                    Result.CourseID = CurrentInput.CourseId;
                    Result.LocationID = Locations.Where(Loc => Loc.Code == CurrentInput.LocationId).First().Id;
                    Result.Cancelled = false;
                    Result.StartTime = ValidStartDate - ValidStartDate;
                    Result.EndTime = ValidStartDate - ValidStartDate;
                    Result.StartDate = ValidStartDate;
                    Result.EndDate = ValidStartDate.AddDays(CurrentInput.LengthDays - 1);
                    Result.RequestType = "Optimizer";
                    Result.Requester = "Optimizer";
                    Result.Hidden = true;
                    Result.CreationTimestamp = DateTime.Today;
                    Result.AttendanceLocked = false;

                    Console.WriteLine($"The course will be scheduled from {ValidStartDate.ToString(TIME_FORMAT)}" +
                        $" to {ValidStartDate.AddDays(CurrentInput.LengthDays - 1).ToString(TIME_FORMAT)}  with the room and instructor listed above.");
                    Console.WriteLine();

                    // Add to the result container
                    Results.Add(Result);

                    // Keep running if the same course needs to be scheduled additional times
                    if (currentIterationForThisInput >= CurrentInput.NumTimesToRun)
                    {
                        Result = new OptimizerResult
                        {
                            InstrUsername = "",
                            RoomID = 0
                        };
                    }
                    // Otherwise, continue to the next input
                    else
                    {
                        break;
                    }
                }
            }
            Console.WriteLine("Optimizer Successful Results");
            ConsoleTable.From<OptimizerResultPrintable>(Results.Select(result => new OptimizerResultPrintable(result))).Write(Format.MarkDown);
            Console.WriteLine("");

            if (FailedToSchedule.Count > 0)
            {
                Console.WriteLine("Optimizer Failed Results");
                ConsoleTable.From<OptimizerInput>(FailedToSchedule).Write(Format.MarkDown);
                Console.WriteLine();
            }

            // Add the results to the table

            // Save data to the context

            Console.WriteLine("Optimization Complete.\n");
        }

        /// <summary>
        /// Determines the possible days a course could start on based on the release rate of its location
        /// </summary>
        /// <param name="locationId">The location the class is to be scheduled at</param>
        /// <param name="courseId">The id of the course type being scheduled</param>
        /// <returns>A collection of valid start days for this class</returns>
        private List<DateTime> FindValidStartDates(int locationId, int classLengthDays, int classMaxSize, int? releaseRate)
        {
            // Check if releaseRate was nullable from the table
            if (!releaseRate.HasValue)
            {
                throw new Exception($"The location with the id: {locationId}, has a null value from the database. The optimizer cannot calcualte without a release rate");
            }

            // Create container for the answers
            var ValidStartDates = new List<DateTime>();

            // Variable used to skip days when a date exceeds the release rate
            // Example: Day 2 of a 5 day course starting on a monday will break the release rate
            // Skip the starting day to Wednesday because we already know Tuesday won't work
            int DaysToSkip = 0; 

            // Loop through each possible start date for this class, from the beginning of the optimizer range
            // until the last day the class can start and end within the range
            foreach(var FirstDay in EachWeekDay(StartDate, AddWeekDays(EndDate, classLengthDays - 1, true)))
            {
                // If there are days to skip, decrement the counter and continue
                if (DaysToSkip-- > 0)
                    continue;
                // Flag used to flip if the range doesn't work
                bool ReleaseRateSatisfied = true;
                // Loop through the days the course would take place 
                foreach(var CurrentDay in EachWeekDay(FirstDay, AddWeekDays(FirstDay, classLengthDays - 1)))
                {
                    // If adding the class size would exceed the release rate at this location, we can't schedule the course in this range
                    if (CurrentlyReleased[LocationIndexMap[locationId], DateIndexMap[CurrentDay.ToString(TIME_FORMAT)]] + classMaxSize > releaseRate)
                    {
                        // Set how many days to skip so that the foreach starts on the day after this one
                        // Example, we started checking from a Monday for a 5 day class, and Wednesday won't work, so we will set DaysToSkip to 2
                        // and we will skip tuesday and wednesay and start on thursday again which is the next day that the course can be scheduled
                        DaysToSkip = (int)(CurrentDay - FirstDay).TotalDays;
                        ReleaseRateSatisfied = false;
                        break;
                    }
                }
                // Add this day to valid start dates if the whole range was satisfied
                if (ReleaseRateSatisfied)
                {
                    ValidStartDates.Add(FirstDay);
                }
            }
            return ValidStartDates;
        }

        /// <summary>
        /// Determines if an instructor is available for the range of days
        /// </summary>
        /// <param name="instructor">The instructor's username</param>
        /// <param name="validStartDate">The first day of the range</param>
        /// <param name="endDate">The last day of the range</param>
        /// <returns>True if the instructor is available and false if they are not</returns>
        private bool IsInstructorAvailableForDateRange(string instructor, DateTime startDate, DateTime endDate)
        {
            // Loop through all days in the current course range
            foreach (var CurrentDay in EachWeekDay(startDate, endDate))
            {
                // If instructor is unavailable for any day, return false
                if (IsInstructorUnavailable[InstructorIndexMap[instructor], DateIndexMap[CurrentDay.ToString(TIME_FORMAT)]])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines if a room is availble for the range of days
        /// </summary>
        /// <param name="roomID">The room's id</param>
        /// <param name="validStartDate">The first day of the range</param>
        /// <param name="validEndDate">The last day of the range</param>
        /// <returns></returns>
        private bool IsRoomAvailbleForDateRange(int roomID, DateTime startDate, DateTime endDate)
        {
            // Loop through all the days in the current date range
            foreach(var CurrentDay in EachWeekDay(startDate, endDate))
            {
                // If room is unavailable for any day, return false
                if (IsRoomUnavailable[RoomIndexMap[roomID], DateIndexMap[CurrentDay.ToString(TIME_FORMAT)]])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Updates the three matrices to show the unavailability from the new result
        /// </summary>
        /// <param name="validStartDate">The start day for the optimizer result</param>
        /// <param name="validEndDate">The end day for the optimzer result</param>
        /// <param name="locationIdLiteral">The location id </param>
        /// <param name="instrUsername">The instructor username</param>
        /// <param name="roomID">The room id</param>
        private void UpdateMatrices(DateTime validStartDate, DateTime validEndDate, int locationId, int maxClassSize, string instrUsername, int roomID)
        {
            // Go through each day the course will take place
            foreach(var currentDay in EachWeekDay(validStartDate, validEndDate))
            {
                var currentDayString = currentDay.ToString(TIME_FORMAT);
                CurrentlyReleased[LocationIndexMap[locationId], DateIndexMap[currentDayString]] += maxClassSize;
                IsInstructorUnavailable[InstructorIndexMap[instrUsername], DateIndexMap[currentDayString]] = true;
                IsRoomUnavailable[RoomIndexMap[roomID], DateIndexMap[currentDayString]] = true;
            }
        }
    }
}
