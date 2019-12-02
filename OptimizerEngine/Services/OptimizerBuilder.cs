using ConsoleTables;
using LSS.Models;
using MoreLinq;
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
        private DatabaseContext context;
        public bool ShowDebugMessages;
        public Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable = new Dictionary<int, Dictionary<string, bool>>();
        public Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable = new Dictionary<string, Dictionary<string, bool>>();
        public Dictionary<int, Dictionary<string, int>> CurrentlyReleased = new Dictionary<int, Dictionary<string, int>>();
        public Dictionary<int, Dictionary<string, List<int>>> LocallyTaughtCoursesPerDay = new Dictionary<int, Dictionary<string, List<int>>>();
        public Dictionary<string, Dictionary<int, DateTime>> instructorToClassToLastTimeTaught = new Dictionary<string, Dictionary<int, DateTime>>();
        public string TIME_FORMAT = "MM/dd/yyyy";
        public int OriginalInputCount;

        public OptimizerScheduleResults StartingResults { get; internal set; }

        public OptimizerEngineBuilder(DatabaseContext _context, DateTime start, DateTime end, Priority priority, bool debug = false)
        {
            ShowDebugMessages = debug;
            StartDate = start;
            EndDate = end;
            Engine = new OptimizerEngine();
            Engine.MyPriority = priority;
            context = _context;
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
            Engine.context = context;

            // Pull in instructors
            Engine.Instructors = context.User.Where(user => user.RoleID == 3).ToList();

            // Course Catalog
            Engine.CourseCatalog = context.Course.ToList();

            // Add to each course the instructors qualified to teach them
            context.InstructorStatus.
                Where(status => status.Deleted == false && status.Qualification == 1 && Engine.Instructors.Any(instr => instr.Username == status.InstructorID)).ToList().
                ForEach(status => Engine.CourseCatalog.
                Where(course => course.ID == status.CourseID)
                .First().QualifiedInstructors.Add(status.InstructorID, DateTime.MinValue));

            // Add to each instructor the amount of courses they can teach
            context.InstructorStatus.
                Where(status => status.Deleted == false && status.Qualification == 1 && Engine.Instructors.Any(y => y.Username == status.InstructorID)).ToList().
                ForEach(status => Engine.Instructors.Where(x => x.Username == status.InstructorID).FirstOrDefault().QualificationCount++);

            // Add to each course the required resources it needs from a room
            context.CourseRequiredResources.ToList().
                ForEach(required => Engine.CourseCatalog.Where(course => course.ID == required.CourseID).
                FirstOrDefault().RequiredResources[required.ResourceID] = required.Amount);

            // Rooms
            Engine.Rooms = context.Room.ToDictionary(room => room.ID, room => room);
            // Add to each room the resources it has
            context.RoomHasResources.ToList().ForEach(resources => Engine.Rooms.
            Where(room => room.Value.ID == resources.RoomID).FirstOrDefault().Value.
            Resources_dict[resources.ResourceID] = resources.Amount);


            // Get the optimizer input data
            Engine.Inputs = (from input in context.OptimizerInput
                        join course in context.Course
                        on input.CourseCode equals course.Code
                        join location in context.Location
                        on input.LocationID equals location.Code
                        where input.Succeeded == false && input.Selected == true
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
                            Reason = "",
                            LengthDays = Math.Max(course.Hours / 8, 1),
                            CourseId = (int)course.ID,
                            LocationIdLiteral = location.ID,
                            RemainingRuns = input.NumTimesToRun,
                            Selected = input.Selected
                        }).ToList<OptimizerInput>();

            OriginalInputCount = Engine.Inputs.Count;

            // Scheduled/Planned classes
            // Only grab classes within the optimzer time range and that aren't cancelled
            Engine.CurrentSchedule = context.ScheduledClass
                .Where(c => c.Cancelled == false && (
                (DateTime.Compare(c.StartDate, Engine.StartDate) >= 0 && DateTime.Compare(c.StartDate, Engine.EndDate) <= 0) ||
                (DateTime.Compare(c.EndDate, Engine.StartDate) >= 0 && DateTime.Compare(c.EndDate, Engine.EndDate) <= 0) ||
                (DateTime.Compare(c.StartDate, Engine.StartDate) < 0 && DateTime.Compare(c.EndDate, Engine.EndDate) > 0)
                )).ToList();


            // Instructor assignments
            Engine.InstructorAssignments = context.InstructorOfClass
                    .Where(i => i.Cancelled == false && (
                    (DateTime.Compare(i.StartDate, Engine.StartDate) >= 0 && DateTime.Compare(i.StartDate, Engine.EndDate) <= 0) ||
                    (DateTime.Compare(i.EndDate, Engine.StartDate) >= 0 && DateTime.Compare(i.EndDate, Engine.EndDate) <= 0) ||
                    (DateTime.Compare(i.StartDate, Engine.StartDate) < 0 && DateTime.Compare(i.EndDate, Engine.EndDate) > 0)
                    )).ToList();

            // Remove instructor assignments if the instructor is not in the user table
            Engine.InstructorAssignments.RemoveAll(assignment => !Engine.Instructors.Any(instructor => instructor.Username == assignment.UserID));

            // Remove all assignments if its specified course isn't in the catalog
            Engine.InstructorAssignments.RemoveAll(assignment => !Engine.CourseCatalog.Any(course => Engine.CurrentSchedule.FirstOrDefault(schedule => schedule.ID == assignment.ClassID).CourseID == course.ID));

            // Update instructor assignments by marking them if they are local assignments
            Engine.InstructorAssignments.
                ForEach(Assignment => Assignment.LocalAssignment = (
                Engine.Instructors.Where(instructor => instructor.Username == Assignment.UserID).First().PointID ==
                Engine.CurrentSchedule.Where(Class => Assignment.ClassID == Class.ID).First().LocationID));

            // Exceptions
            Engine.Exceptions = context.Booking
                .Where(b => b.Status == 1 && b.Cancelled == false && (
                (DateTime.Compare(b.StartDate, Engine.StartDate) >= 0 && DateTime.Compare(b.StartDate, Engine.EndDate) <= 0) ||
                (DateTime.Compare(b.EndDate, Engine.StartDate) >= 0 && DateTime.Compare(b.EndDate, Engine.EndDate) <= 0) ||
                (DateTime.Compare(b.StartDate, Engine.StartDate) < 0 && DateTime.Compare(b.EndDate, Engine.EndDate) > 0))
                ).ToList();

            // Locations
            Engine.Locations = context.Location.ToList();
            // Populate the local rooms for each location
            context.Room.ToList().ForEach(room => Engine.Locations.Find(location => location.Code.Equals(room.Station)).LocalRooms.Add(room.ID));
            // Populate the local instructs for each location
            Engine.Instructors.ForEach(instructor => Engine.Locations.First(location => location.ID == instructor.PointID).LocalInstructors.Add(instructor.Username));

            if (ShowDebugMessages)
            {
                Console.WriteLine("Optimizer Inputs");
                ConsoleTable.From<OptimizerInputPrintable>(Engine.Inputs.Select(x => new OptimizerInputPrintable(x))).Write(Format.MarkDown);
                Console.WriteLine("");

                // Assign to each scheduled class its instructor, for printing purposes
                Engine.InstructorAssignments.ForEach(assignment => Engine.CurrentSchedule.First(classEvent => classEvent.ID == assignment.ClassID)
                .Instructor = Engine.Instructors.First(instructor => instructor.Username == assignment.UserID));

                Engine.CurrentSchedule.ForEach(classEvent => classEvent.CourseCode = Engine.CourseCatalog.First(course => course.ID == classEvent.CourseID).Code);
                Engine.CurrentSchedule.ForEach(classEvent => classEvent.Location = Engine.Locations.First(loc => loc.ID == classEvent.LocationID).Code);

                Console.WriteLine("Current Schedule");
                ConsoleTable.From<ScheduledClassPrintable>(Engine.CurrentSchedule.Select(c => new ScheduledClassPrintable(c))).Write(Format.MarkDown);
                Console.WriteLine("");

                Console.WriteLine("Instructor Assignments");
                ConsoleTable.From<InstructorOfClass>(Engine.InstructorAssignments).Write(Format.MarkDown);
                Console.WriteLine("");

                Console.WriteLine("Bookings");
                ConsoleTable.From<BookingPrintable>(Engine.Exceptions.Select(e => new BookingPrintable(e))).Write(Format.MarkDown);
                Console.WriteLine("");

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
                List<string> Headers;
                ConsoleTable table;
                foreach(var sublist in OptimizerUtilities.Split(WeekDaysInRange, 20))
                {
                    Headers = new List<string> { "RoomID" };
                    sublist.ForEach(day => Headers.Add(day.Substring(0, day.Length - 5)));
                    table = new ConsoleTable(Headers.ToArray());
                    foreach (var RoomPair in IsRoomUnavailable)
                    {
                        var row = new List<string> { RoomPair.Key.ToString() };
                        RoomPair.Value.ToList().ForEach(pair =>
                        {
                            if (sublist.Contains(pair.Key))
                                    row.Add((pair.Value ? "Yes" : ""));
                        });
                        table.AddRow(row.ToArray());
                    }
                    Console.WriteLine("IsRoomUnavailable");
                    table.Write(Format.MarkDown);
                }

                foreach (var sublist in OptimizerUtilities.Split(WeekDaysInRange, 20))
                {
                    Headers = new List<string> { "Location" };
                    sublist.ForEach(day => Headers.Add(day.Substring(0, day.Length - 5)));
                    table = new ConsoleTable(Headers.ToArray());
                    foreach (var LocPair in CurrentlyReleased)
                    {
                        var row = new List<string> { Engine.Locations.First(loc => loc.ID == LocPair.Key).Code };
                        LocPair.Value.ToList().ForEach(pair =>
                        {
                            if (sublist.Contains(pair.Key))
                                row.Add((pair.Value > 0 ? pair.Value.ToString() : ""));
                        });
                        table.AddRow(row.ToArray());
                    }
                    Console.WriteLine("CurrentlyReleased");
                    table.Write(Format.MarkDown);
                }

                foreach (var sublist in OptimizerUtilities.Split(WeekDaysInRange, 20))
                {
                    Headers = new List<string> { "Location" };
                    sublist.ForEach(day => Headers.Add(day.Substring(0, day.Length - 5)));
                    table = new ConsoleTable(Headers.ToArray());
                    foreach (var Location in LocallyTaughtCoursesPerDay)
                    {
                        var row = new List<string> { Engine.Locations.First(loc => loc.ID == Location.Key).Code };
                        Location.Value.Where(pair => sublist.Contains(pair.Key)).ToList().ForEach(list => 
                        {
                            row.Add(string.Join(", ", list.Value));
                        });
                        table.AddRow(row.ToArray());
                    }
                    Console.WriteLine("Locally taught courses at each location");
                    table.Write(Format.MarkDown);
                }
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
                    IsInstructorUnavailable[exception.RequestForID][date.ToString(TIME_FORMAT)] = true;
                }
            }
            if (ShowDebugMessages)
            {
                List<string> Headers;
                ConsoleTable table;
                foreach (var sublist in OptimizerUtilities.Split(WeekDaysInRange, 20))
                {
                    Headers = new List<string> { "Username" };
                    sublist.ForEach(day => Headers.Add(day.Substring(0, day.Length - 5)));
                    table = new ConsoleTable(Headers.ToArray());
                    foreach (var InstructorPair in IsInstructorUnavailable)
                    {
                        var row = new List<string> { InstructorPair.Key.ToString() };
                        InstructorPair.Value.ToList().ForEach(pair =>
                        {
                            if (sublist.Contains(pair.Key))
                                row.Add((pair.Value ? "Yes" : ""));
                        });
                        table.AddRow(row.ToArray());
                    }
                    Console.WriteLine("IsInstructorUnavailable");
                    table.Write(Format.MarkDown);
                }
            }

            // Set the last day each instructor taught a course if the priority is set
            if (Engine.MyPriority == Priority.MaximizeInstructorLongestToTeach)
            {
                var CompleteSchedule = context.ScheduledClass.ToArray();
                // Obtain every instructor assignment in the database
                var completeAssignments = context.InstructorOfClass.OrderBy(i => i.StartDate).ToArray();
                Console.WriteLine("Calculating the last time each instructor taught a course...");
                foreach(var a in completeAssignments)
                {
                    if (CompleteSchedule.Any(s => s.ID == a.ClassID))
                    {
                        // grab the class that corresponds to the assignment
                        ScheduledClass correspondingClass = CompleteSchedule.First(s => s.ID == a.ClassID);
                        if (Engine.CourseCatalog.Any(c => c.ID == correspondingClass.CourseID))
                        {
                            // get the course info 
                            var CourseInfo = Engine.CourseCatalog.First(c => c.ID == CompleteSchedule.First(s => s.ID == a.ClassID).CourseID);
                            if (CourseInfo.QualifiedInstructors.ContainsKey(a.UserID))
                            {
                                // if this assignment ends after the last recorded time the instructor taught this course, update 
                                // the last time taugth the course to the end of this assignment
                                if (DateTime.Compare(CourseInfo.QualifiedInstructors[a.UserID], a.EndDate) < 0)
                                    CourseInfo.QualifiedInstructors[a.UserID] = a.EndDate;
                            }
                        }
                    }
                }
                // Limit Course catalog to courses just listed in the input
                Engine.CourseCatalog = Engine.CourseCatalog.Where(c => Engine.Inputs.Any(i => i.CourseId == c.ID)).ToList();

                Console.WriteLine("Done.");
                if (ShowDebugMessages)
                {
                    List<string> Headers;
                    ConsoleTable table;
                    Headers = new List<string> { "CourseCode" };
                    Headers.Add("Instructor FileNumber: Last Taught");
                    table = new ConsoleTable(Headers.ToArray());
                    foreach (var Course in Engine.CourseCatalog)
                    {
                        foreach(var sublist in OptimizerUtilities.Split(Course.QualifiedInstructors.ToList(), 8))
                        {
                            var row = new List<string> { Course.Code.ToString() };
                            var rowString = "";
                            Course.QualifiedInstructors.ToList().ForEach(p => {
                                if (sublist.Contains(p))
                                    if (p.Value == DateTime.MinValue)
                                        rowString += $"{p.Key}: {"Never Taught"}; ";
                                    else
                                        rowString += $"{p.Key}: {p.Value.ToString("MM/dd/yyyy")}; ";
                            }
                            );
                            row.Add(rowString);
                            table.AddRow(row.ToArray());
                        }
                    }
                    Console.WriteLine("Last Time A Course Was Taught By Who");
                    table.Write(Format.MarkDown);
                    
                }

            }


            if (ShowDebugMessages) Console.WriteLine("Data loading complete.\n");

            if (ShowDebugMessages) Console.WriteLine("Checking if any inputs are impossible...");
            Engine.PrimeStartingResults(ref IsRoomUnavailable, ref IsInstructorUnavailable,
                    ref CurrentlyReleased, ref LocallyTaughtCoursesPerDay);
            StartingResults = new OptimizerScheduleResults()
            {
                Results = new List<OptimizerResult>(),
                Inputs = Engine.Inputs,
                OptimizationScore = 0
            };
            Engine.InputCount = Engine.Inputs.Count;
            var Levels = 0;
            Engine.Inputs.ForEach(x => Levels += Math.Min(x.MaxPossibleIterations, x.NumTimesToRun));
            Engine.NodesPerDepth = new int[Levels];
            if (ShowDebugMessages) Console.WriteLine("Done.\n");

            if (ShowDebugMessages)
            {
                Console.WriteLine("Optimizer Possible Inputs");
                ConsoleTable.From<OptimizerInputPrintable>(Engine.Inputs.Select(x => new OptimizerInputPrintable(x))).Write(Format.MarkDown);
                Console.WriteLine("");
            }


            if (ShowDebugMessages) Console.Write("Predicting the best possible score for the engine...");

            // Setup the best answer as something guarenteed to always be the worst
            Engine.CurrentBestAnswer = new OptimizerScheduleResults
            {
                Results = new List<OptimizerResult>(),
                Inputs = new List<OptimizerInput>(),
                OptimizationScore = -1
            };

            if (Engine.MyPriority == Priority.MinimizeForeignInstructorCount || Engine.MyPriority == Priority.MinimizeInstructorTravelDistance)
                Engine.CurrentBestAnswer.OptimizationScore = int.MaxValue;

            switch (Engine.MyPriority)
            {
                case Priority.MaximizeInstructorLongestToTeach:
                    Engine.BestPossibleScore = int.MaxValue;
                    break;
                case (Priority.MaximizeSpecializedInstructors):
                    Engine.BestPossibleScore = Engine.Instructors.MinBy(i => i.QualificationCount).First().QualificationCount * Engine.InputCount;
                    if (Engine.BestPossibleScore == 0) Engine.BestPossibleScore = Engine.InputCount;
                    break;
                case (Priority.MinimizeForeignInstructorCount):
                    // Set the best answer to 0, meaning 0 instructors to travel
                    Engine.BestPossibleScore = Levels;
                    // Increment the best possible answer for every input that has no qualified local instructors
                    foreach (var input in Engine.Inputs)
                    {
                        var CourseInfo = Engine.CourseCatalog.Where(course => course.ID == input.CourseId).First();
                        if (Engine.Locations.First(z => z.ID == input.LocationIdLiteral).LocalInstructors.Any(x => CourseInfo.QualifiedInstructors.Any(y => x == y.Key)))
                        {
                            Console.WriteLine($"subtracting {input.NumTimesToRun} from the score because of input {input.Id}");
                            Engine.BestPossibleScore -= input.NumTimesToRun;
                        }
                    }
                    break;
                case (Priority.MinimizeInstructorTravelDistance):
                    // Set the best answer to 0, meaning 0 instructors to travel
                    Engine.BestPossibleScore = 0;
                    break;
                default:
                    Engine.BestPossibleScore = Engine.NodesPerDepth.Length;
                    break;
            }

            if (ShowDebugMessages) Console.WriteLine($"{Engine.BestPossibleScore}. Done.\n");


            return Engine;
        }
    }
}
