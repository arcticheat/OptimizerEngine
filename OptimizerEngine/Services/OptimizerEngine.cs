using ConsoleTables;
using LSS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LSS.Services
{
    public class OptimizerEngine
    {
        public List<OptimizerInput> Inputs;
        public List<ScheduledClass> CurrentSchedule;
        public List<InstructorOfClass> InstructorAssignments;
        public List<User> Instructors;
        public List<Booking> Exceptions;
        public List<Location> Locations;
        public List<Course> CourseCatalog;
        public Dictionary<int, Room> Rooms;
        public Dictionary<String, int> DateIndexMap;
        public Dictionary<int, int> RoomIndexMap;
        public Dictionary<string, int> InstructorIndexMap;
        public Dictionary<int, int> LocationIndexMap;
        public bool[,] IsRoomUnavailable;
        public bool[,] IsInstructorUnavailable;
        public int[,] CurrentlyReleased;
        public string TIME_FORMAT = "MM/dd/yyyy";
        public DateTime StartDate;
        public DateTime EndDate;

        private DatabaseContext context;

        public OptimizerEngine(DatabaseContext _context)
        {
            context = _context;
        }

        /// <summary>
        /// The optimizer will calculate a single collection of optimizer results created from the optimizer input
        /// The results are created by ensuring the schedule is legal as well as assigning the resources on a first available focus
        /// </summary>
        public void OptimizeGreedy()
        {
            Console.WriteLine("Optimizing...\n");
            // Container for the results
            var Results = new List<OptimizerResult>();

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
                var MaxClassSize = CourseCatalog.Where(course => course.ID == CurrentInput.CourseId).First().MaxSize;
                var ReleaseRate = Locations.Where(location => location.ID == CurrentInput.LocationIdLiteral).First().ReleaseRate;

                // Obtain the information about this course in the catalog
                var CourseInfo = CourseCatalog.Where(course => course.ID == CurrentInput.CourseId).First();

                // Obtain all possible start dates (restricted by location release rate and course length)
                var ValidStartDates = FindValidStartDates(CurrentInput.LocationIdLiteral, CurrentInput.LengthDays, MaxClassSize, ReleaseRate);
                if (ValidStartDates.Count <= 0)
                {
                    Console.WriteLine($"The input could not be scheduled because the location {CurrentInput.LocationID} would exceed its release rate.\n");
                    CurrentInput.Reason = "Release rate would be exceeded.";
                }

                // Counter to keep track of inputs that need multiple iterations scheduled 
                var currentIterationForThisInput = 1;

                // Loop through each day within the optimizer range that the course could start on
                var lastDate = ValidStartDates.LastOrDefault();
                
                foreach (var ValidStartDate in ValidStartDates)
                {
                    Console.WriteLine($"Searching on the start date {ValidStartDate.ToString(TIME_FORMAT)} for an instructor and room.");
                    // Set the end date for this range based off of the course length
                    var ValidEndDate = ValidStartDate.AddDays(CurrentInput.LengthDays - 1);

                    // Loop through all qualified instructors for this course
                    foreach (var Instructor in CourseInfo.QualifiedInstructors)
                    {
                        // Determine if this instructor is available for the range
                        if (IsInstructorAvailableForDateRange(Instructor, ValidStartDate, ValidEndDate))
                        {
                            Console.WriteLine($"The instructor {Instructor} is available.");
                            Result.InstrUsername = Instructor;
                            // Set the result status to using a local instructor
                            Result.UsingLocalInstructor = Instructors.Where(instr => instr.Username == Instructor).First().PointID == CurrentInput.LocationIdLiteral;
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
                        }
                        continue;
                    }
                    // Loop through all local rooms for this location
                    // but only the rooms that have the right type and quantity of resources required by this course type
                    foreach (var RoomID in Locations.Where(Loc => Loc.Code == CurrentInput.LocationID).First().
                        LocalRooms.Where(room => CourseInfo.RequiredResources.All(required =>
                        Rooms[room].Resources_dict.ContainsKey(required.Key) && Rooms[room].Resources_dict[required.Key] >= required.Value )))
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
                        }
                        continue;
                    }
                    // If successful, update the matrices 
                    UpdateMatrices(ValidStartDate, ValidEndDate, CurrentInput.LocationIdLiteral, MaxClassSize, Result.InstrUsername, Result.RoomID, Result.UsingLocalInstructor);

                    // Found an answer so set the remaining fields for the result
                    Result.CourseID = CurrentInput.CourseId;
                    Result.LocationID = Locations.Where(Loc => Loc.Code == CurrentInput.LocationID).First().ID;
                    Result.Cancelled = false;
                    Result.StartTime = ValidStartDate - ValidStartDate;
                    Result.EndTime = ValidStartDate - ValidStartDate;
                    Result.StartDate = ValidStartDate;
                    Result.EndDate = ValidStartDate.AddDays(CurrentInput.LengthDays - 1);
                    Result.RequestType = "Optimizer";
                    Result.Requester = "Optimizer";
                    Result.Hidden = true;
                    Result.AttendanceLocked = false;

                    Console.WriteLine($"The course will be scheduled from {ValidStartDate.ToString(TIME_FORMAT)}" +
                        $" to {ValidStartDate.AddDays(CurrentInput.LengthDays - 1).ToString(TIME_FORMAT)}  with the room and instructor listed above.");
                    Console.WriteLine();

                    // Add to the result container
                    Results.Add(Result);

                    // Keep running if the same course needs to be scheduled additional times
                    if (currentIterationForThisInput++ < CurrentInput.NumTimesToRun)
                    {
                        Result = new OptimizerResult
                        {
                            InstrUsername = "",
                            RoomID = 0
                        };
                        Console.WriteLine("This input is required to be scheduled again. Continuing from next valid start date...");
                    }
                    // Otherwise, continue to the next input
                    else
                    {
                        CurrentInput.Succeeded = true;
                        break;
                    }
                }
            }
            Console.WriteLine("Optimizer Successful Results");
            ConsoleTable.From<OptimizerResultPrintable>(Results.Select(result => new OptimizerResultPrintable(result))).Write(Format.MarkDown);
            Console.WriteLine("");

            if (Inputs.Count(input => !input.Succeeded) > 0)
            {
                Console.WriteLine("Optimizer Failed Results");
                ConsoleTable.From<OptimizerInput>(Inputs.Where(input => !input.Succeeded)).Write(Format.MarkDown);
                Console.WriteLine();
            }


            // Add the results to the table
            foreach(var result in Results)
            {
                result.CreationTimestamp = DateTime.Today;
                context.Entry(result).State = result.ID == 0 ? EntityState.Added : EntityState.Modified;
            }
            foreach(var input in Inputs)
            {
                context.Entry(input).State = input.Id == 0 ? EntityState.Added : EntityState.Modified;
            }

            // Save data to the context
            context.SaveChanges();



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
            foreach(var FirstDay in OptimizerUtilities.EachWeekDay(StartDate, OptimizerUtilities.AddWeekDays(EndDate, classLengthDays - 1, true)))
            {
                // If there are days to skip, decrement the counter and continue
                if (DaysToSkip-- > 0)
                    continue;
                // Flag used to flip if the range doesn't work
                bool ReleaseRateSatisfied = true;
                // Loop through the days the course would take place 
                foreach(var CurrentDay in OptimizerUtilities.EachWeekDay(FirstDay, OptimizerUtilities.AddWeekDays(FirstDay, classLengthDays - 1)))
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
            foreach (var CurrentDay in OptimizerUtilities.EachWeekDay(startDate, endDate))
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
            foreach(var CurrentDay in OptimizerUtilities.EachWeekDay(startDate, endDate))
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
        private void UpdateMatrices(DateTime validStartDate, DateTime validEndDate, int locationId, int maxClassSize, string instrUsername, int roomID, bool localAssignment)
        {
            // Go through each day the course will take place
            foreach(var currentDay in OptimizerUtilities.EachWeekDay(validStartDate, validEndDate))
            {
                var currentDayString = currentDay.ToString(TIME_FORMAT);
                CurrentlyReleased[LocationIndexMap[locationId], DateIndexMap[currentDayString]] += maxClassSize;
                IsInstructorUnavailable[InstructorIndexMap[instrUsername], DateIndexMap[currentDayString]] = true;
                IsRoomUnavailable[RoomIndexMap[roomID], DateIndexMap[currentDayString]] = true;
            }

            if (!localAssignment)
            {
                // Check if the day before the start day for the classs is in the index map
                // If its not, it is either an adjacent week day but outside the range of the optimizer
                // Or it is a weekend (the day before monday is sunday, will not be mapped in the dictionary
                var DayBeforeAssignment = OptimizerUtilities.Max(validStartDate, StartDate).AddDays(-1).ToString(TIME_FORMAT);
                if (DateIndexMap.ContainsKey(DayBeforeAssignment))
                {
                    IsInstructorUnavailable[InstructorIndexMap[instrUsername], DateIndexMap[DayBeforeAssignment]] = true;
                }
                // Do same but for the day after
                var DayAfterAssignment = OptimizerUtilities.Min(validEndDate, EndDate).ToString(TIME_FORMAT);
                if (DateIndexMap.ContainsKey(DayAfterAssignment))
                {
                    IsInstructorUnavailable[InstructorIndexMap[instrUsername], DateIndexMap[DayAfterAssignment]] = true;
                }
            }
        }
    }
}
