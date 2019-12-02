using ConsoleTables;
using LSS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;
using MoreLinq;
using System.Threading;
using System.Threading.Tasks;

namespace LSS.Services
{
    public enum Priority
    {
        Default, MinimizeForeignInstructorCount, MinimizeInstructorTravelDistance, MaximizeSpecializedInstructors,
        MaximizeInstructorLongestToTeach, FirstAvailable
    }
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
        public string TIME_FORMAT = "MM/dd/yyyy";
        public DateTime StartDate;
        public DateTime EndDate;
        public bool ShowDebugMessages;
        public int InputCount;
        public DatabaseContext context;
        public OptimizerScheduleResults CurrentBestAnswer;
        private int TotalSchedulesCreated = 0;
        private int NonEndNodesThatReturned = 0;
        public int TotalWeekDays;
        public int[] NodesPerDepth;
        public Priority MyPriority;
        private bool ABestAnswerFound = false;
        public int BestPossibleScore;
        public List<OptimizerInput> WillAlwaysFail = new List<OptimizerInput>();

        /// <summary>
        /// The optimizer will calculate a single collection of optimizer results created from the optimizer input
        /// The results are created by ensuring the schedule is legal as well as assigning the resources on a first available focus
        /// </summary>
        internal OptimizerScheduleResults OptimizeGreedy(Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable, Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable,
            Dictionary<int, Dictionary<string, int>> CurrentlyReleased, Dictionary<int, Dictionary<string, List<int>>> LocallyTaughtCoursesPerDay)
        {
            // Struct for the answr
            var Answer = new OptimizerScheduleResults()
            {
                Results = new List<OptimizerResult>(),
                Inputs = OptimizerUtilities.DeepClone(this.Inputs),
                OptimizationScore = 0
            };

            // Loop through each input from the optimizer
            foreach (var CurrentInput in Answer.Inputs)
            {
                if (ShowDebugMessages) Console.WriteLine($"Calculating result for input ID {CurrentInput.Id}... ");

                // Obtain the class max size and the location release rate for the function call to find the valid start dates
                var MaxClassSize = CourseCatalog.Where(course => course.ID == CurrentInput.CourseId).First().MaxSize;
                var ReleaseRate = Locations.Where(location => location.ID == CurrentInput.LocationIdLiteral).First().ReleaseRate;

                // Obtain the information about this course in the catalog
                var CourseInfo = CourseCatalog.Where(course => course.ID == CurrentInput.CourseId).First();
                var reason = "";

                // Obtain all possible start dates (restricted by location release rate and course length)
                var ValidStartDates = FindValidStartDates(CurrentInput.LocationIdLiteral, CurrentInput.LengthDays, MaxClassSize, ReleaseRate,
                    (int)CourseInfo.ID, ref CurrentlyReleased, ref LocallyTaughtCoursesPerDay, ref reason);
                if (ValidStartDates.Count <= 0)
                {
                    CurrentInput.Reason = reason;
                }

                // Counter to keep track of inputs that need multiple iterations scheduled 
                var currentIterationForThisInput = 0;

                // Loop through each day within the optimizer range that the course could start on
                var lastDate = ValidStartDates.LastOrDefault();
                
                foreach (var ValidStartDate in ValidStartDates)
                {
                    if (LocallyTaughtCoursesPerDay[CurrentInput.LocationIdLiteral][ValidStartDate.ToString(TIME_FORMAT)].Contains((int)CourseInfo.ID))
                    {
                        if (ValidStartDate == lastDate)
                        {
                            CurrentInput.Reason = $"Could only schedule {currentIterationForThisInput} out of {CurrentInput.NumTimesToRun} requests";
                        }
                        continue;
                    }

                    // result object for this input
                    var Result = new OptimizerResult
                    {
                        InstrUsername = "",
                        RoomID = 0
                    };

                    if (ShowDebugMessages) Console.WriteLine($"Searching on the start date {ValidStartDate.ToString(TIME_FORMAT)} for an instructor and room.");

                    // Set the end date for this range based off of the course length
                    var ValidEndDate = Utilities.getNextBusinessDate(ValidStartDate, CurrentInput.LengthDays - 1);

                    // Loop through all qualified instructors for this course
                    foreach (var Instructor in CourseInfo.QualifiedInstructors)
                    {
                        // Determine if this instructor is available for the range
                        if (IsInstructorAvailableForDateRange(Instructor.Key, ValidStartDate, ValidEndDate, IsInstructorUnavailable))
                        {
                            if (ShowDebugMessages) Console.WriteLine($"The instructor {Instructor} is available.");
                            Result.InstrUsername = Instructor.Key;
                            // Set the result status to using a local instructor
                            Result.UsingLocalInstructor = Instructors.Where(instr => instr.Username == Instructor.Key).First().PointID == CurrentInput.LocationIdLiteral;
                            break;
                        }
                    }
                    // If no instructor is available, consider the next start date
                    if (Result.InstrUsername == "")
                    {
                        if (ShowDebugMessages) Console.WriteLine("No instructors are available. Moving on to the next valid start date.");
                        if (ValidStartDate == lastDate)
                        {
                            if (ShowDebugMessages) Console.WriteLine($"The input could not be scheduled because no instructor is available.\n");
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
                        if (IsRoomAvailbleForDateRange(RoomID, ValidStartDate, ValidEndDate, IsRoomUnavailable))
                        {
                            if (ShowDebugMessages) Console.WriteLine($"The Room {RoomID} is available.");
                            Result.RoomID = RoomID;
                            break;
                        }
                    }
                    // If no room is available, consider the next start date
                    if (Result.RoomID == 0)
                    {
                        if (ShowDebugMessages) Console.WriteLine("No room is available. Moving on to the next valid start date.");
                        Result.InstrUsername = "";
                        if (ValidStartDate == lastDate)
                        {
                            if (ShowDebugMessages) Console.WriteLine($"The above input could not be scheduled because no room is available.\n");
                            CurrentInput.Reason = "No local room is available.";
                        }
                        continue;
                    }
                    // If successful, update the matrices 
                    UpdateMatrices(ValidStartDate, ValidEndDate, CurrentInput.LocationIdLiteral, MaxClassSize, Result.InstrUsername, Result.RoomID, Result.UsingLocalInstructor,
                        (int)CourseInfo.ID, ref CurrentlyReleased, ref IsInstructorUnavailable, ref IsRoomUnavailable, ref LocallyTaughtCoursesPerDay);

                    // Found an answer so set the remaining fields for the result
                    Result.CourseID = CurrentInput.CourseId;
                    Result.LocationID = Locations.Where(Loc => Loc.Code == CurrentInput.LocationID).First().ID;
                    Result.Cancelled = false;
                    Result.StartTime = CurrentInput.StartTime;
                    Result.EndTime = CurrentInput.StartTime.Add(new TimeSpan(Math.Min(8, CourseInfo.Hours), 0, 0));
                    Result.StartDate = ValidStartDate;
                    Result.EndDate = ValidEndDate;
                    Result.RequestType = "Optimizer";
                    Result.Requester = "Optimizer";
                    Result.Hidden = true;
                    Result.AttendanceLocked = false;
                    Result.Location = CurrentInput.LocationID;
                    Result.CourseCode = CurrentInput.CourseCode;

                    if (ShowDebugMessages)
                    {
                        Console.WriteLine($"The course will be scheduled from {ValidStartDate.ToString(TIME_FORMAT)}" +
                        $" to {ValidEndDate.ToString(TIME_FORMAT)}  with the room and instructor listed above.");
                        Console.WriteLine();
                    }

                    // Add to the result container
                    Answer.Results.Add(Result);

                    // Keep running if the same course needs to be scheduled additional times
                    if (currentIterationForThisInput++ < CurrentInput.NumTimesToRun - 1)
                    {
                        if (ValidStartDate == lastDate)
                        {
                            CurrentInput.Reason = $"Could only schedule {currentIterationForThisInput} out of {CurrentInput.NumTimesToRun} requests";
                            if (ShowDebugMessages) Console.WriteLine("This input is required to be schedule again, but no more days are available.\n");
                        }
                        else
                        {
                            
                            if (ShowDebugMessages) Console.WriteLine("This input is required to be scheduled again. Continuing from next valid start date...");
                        }
                    }
                    // Otherwise, continue to the next input
                    else
                    {
                        CurrentInput.Succeeded = true;
                        break;
                    }
                }
            }
            return Answer;
        }

        internal OptimizerScheduleResults OptimizeRecursion(OptimizerScheduleResults IncomingResults, int InputIndex,
            Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable, Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable,
            Dictionary<int, Dictionary<string, int>> CurrentlyReleased,
            Dictionary<int, Dictionary<string, List<int>>> LocallyTaughtCoursesPerDay,
            int CurrentDepth)
        {
            // Always check if the best answer is already found
            if (IsABestAnswerFound())
            {
                return CurrentBestAnswer;
            }

            // Predict if a better answer is even possible from here
            // First see if adding every single remaining class for the remainder of this branch would 
            // create a result with more successful results than the best
            if (NodesPerDepth.Length - CurrentDepth + IncomingResults.Results.Count < CurrentBestAnswer.Results.Count)
            {
                switch (MyPriority)
                {
                    case Priority.MaximizeSpecializedInstructors:
                        if (IncomingResults.OptimizationScore >= CurrentBestAnswer.OptimizationScore)
                            return CurrentBestAnswer;
                        break;
                    case Priority.MinimizeForeignInstructorCount:
                        if (IncomingResults.OptimizationScore >= CurrentBestAnswer.OptimizationScore)
                            return CurrentBestAnswer;
                        break;
                    case Priority.MinimizeInstructorTravelDistance:
                        if (IncomingResults.OptimizationScore >= CurrentBestAnswer.OptimizationScore)
                            return CurrentBestAnswer;
                        break;
                    default:
                        if (NodesPerDepth.Length - CurrentDepth + IncomingResults.Results.Count <=
                            CurrentBestAnswer.Results.Count)
                            return CurrentBestAnswer;
                        break;
                }
            }

            // base case if there are no more inputs
            if (InputIndex >= InputCount)
            {
                // calculate score
                CalculateScore(IncomingResults);
                // this answer has all its values, return
                TotalSchedulesCreated++;
                return IncomingResults;
            }
            // recursion
            else
            {
                // Obtain the current input information for reference
                var CurrentInput = IncomingResults.Inputs[InputIndex];

                // Container to store all the possible ways to schedule this input
                var PossibleSchedulingsForInput = new List<OptimizerResult>();

                // flags to determine possible reason for failure
                bool NoInstructor = true, NoRoom = true, NoValidStartDates = true;

                // Obtain the class max size and the location release rate for the function call to find the valid start dates
                var MaxClassSize = CourseCatalog.Where(course => course.ID == CurrentInput.CourseId).First().MaxSize;
                var ReleaseRate = Locations.Where(location => location.ID == CurrentInput.LocationIdLiteral).First().ReleaseRate;

                // Obtain the information about this course in the catalog
                var CourseInfo = CourseCatalog.Where(course => course.ID == CurrentInput.CourseId).First();

                // Obtain all possible start dates (restricted by location release rate and course length)
                var reason = "";
                var ValidStartDates = FindValidStartDates(CurrentInput.LocationIdLiteral, CurrentInput.LengthDays, MaxClassSize, ReleaseRate,
                    (int)CourseInfo.ID, ref CurrentlyReleased, ref LocallyTaughtCoursesPerDay, ref reason);

                // Container to hold every child node's answer
                var SubNodeAnswers = new List<OptimizerScheduleResults>();

                // Loop through each day within the optimizer range that the course could start on
                var lastDate = ValidStartDates.LastOrDefault();

                foreach (var ValidStartDate in ValidStartDates)
                {
                    NoValidStartDates = false;
                    // Set the end date for this range based off of the course length
                    var ValidEndDate = Utilities.getNextBusinessDate(ValidStartDate, CurrentInput.LengthDays - 1);

                    // Sort the instructors by the instructors to find the best answer sooner
                    var SortedQualifiedInstructors = new List<KeyValuePair<string, DateTime>>();
                    switch (MyPriority)
                    {
                        case (Priority.MaximizeSpecializedInstructors):
                            SortedQualifiedInstructors = CourseInfo.QualifiedInstructors.
                                OrderBy(x => Instructors.First(y => y.Username == x.Key).QualificationCount).ToList();
                            break;
                        case (Priority.MinimizeForeignInstructorCount):
                            SortedQualifiedInstructors = CourseInfo.QualifiedInstructors.
                                OrderByDescending(x => Locations.First(y => y.ID == CurrentInput.LocationIdLiteral).LocalInstructors.Contains(x.Key)).ToList();
                            break;
                        case (Priority.MinimizeInstructorTravelDistance):
                            SortedQualifiedInstructors = CourseInfo.QualifiedInstructors.
                                OrderByDescending(x => Locations.First(y => y.ID == CurrentInput.LocationIdLiteral).LocalInstructors.Contains(x.Key)).ToList();
                            break;
                        default:
                            SortedQualifiedInstructors = CourseInfo.QualifiedInstructors.ToList();
                            break;
                    }

                    // Loop through all qualified instructors for this course
                    foreach (var Instructor in SortedQualifiedInstructors)
                    {
                        // Determine if this instructor is available for the range
                        if (IsInstructorAvailableForDateRange(Instructor.Key, ValidStartDate, ValidEndDate, IsInstructorUnavailable))
                        {
                            NoInstructor = false;

                            // Loop through all local rooms for this location
                            // but only the rooms that have the right type and quantity of resources required by this course type
                            foreach (var RoomID in Locations.Where(Loc => Loc.Code == CurrentInput.LocationID).First().
                                LocalRooms.Where(room => CourseInfo.RequiredResources.All(required =>
                                Rooms[room].Resources_dict.ContainsKey(required.Key) && Rooms[room].Resources_dict[required.Key] >= required.Value)))
                            {
                                // Determine if this room is available 
                                if (IsRoomAvailbleForDateRange(RoomID, ValidStartDate, ValidEndDate, IsRoomUnavailable))
                                {
                                    NoRoom = false;

                                    // Found an answer so set the remaining fields for the result
                                    // result object for this input
                                    var Result = new OptimizerResult
                                    { 
                                        CourseID = CurrentInput.CourseId,
                                        LocationID = Locations.Where(Loc => Loc.Code == CurrentInput.LocationID).First().ID,
                                        Cancelled = false,
                                        StartTime = CurrentInput.StartTime,
                                        EndTime = CurrentInput.StartTime.Add(new TimeSpan(Math.Min(8, CourseInfo.Hours), 0, 0)),
                                        StartDate = ValidStartDate,
                                        EndDate = ValidEndDate,
                                        RequestType = "Optimizer",
                                        Requester = "Optimizer",
                                        Hidden = true,
                                        AttendanceLocked = false,
                                        Location = CurrentInput.LocationID,
                                        CourseCode = CurrentInput.CourseCode,
                                        RoomID = RoomID,
                                        InstrUsername = Instructor.Key,
                                        UsingLocalInstructor = Instructors.Where(instr => instr.Username == Instructor.Key).First().PointID == CurrentInput.LocationIdLiteral,
                                        inputID = CurrentInput.Id
                                    };

                                    NodesPerDepth[CurrentDepth] += 1;

                                    // Deep copy the current results so this child node will have a unique result object to build from
                                    var MyResults = OptimizerUtilities.DeepClone(IncomingResults);
                                    // Copy all the unavailability data trackers to update them for this recursion
                                    var CRCopy = OptimizerUtilities.DeepClone(CurrentlyReleased);
                                    var IIUCopy = OptimizerUtilities.DeepClone(IsInstructorUnavailable);
                                    var IRUCopy = OptimizerUtilities.DeepClone(IsRoomUnavailable);
                                    var LTCPDCopy = OptimizerUtilities.DeepClone(LocallyTaughtCoursesPerDay);


                                    //update the data container copies with the scheduling info for this result
                                    UpdateMatrices(Result.StartDate, Result.EndDate, CurrentInput.LocationIdLiteral, MaxClassSize, Result.InstrUsername, Result.RoomID, Result.UsingLocalInstructor,
                                        (int)CourseInfo.ID, ref CRCopy, ref IIUCopy, ref IRUCopy, ref LTCPDCopy);

                                    // Add the result
                                    MyResults.Results.Add(Result);
                                    MyResults.Inputs[InputIndex].RemainingRuns -= 1;

                                    // Add the child's answer 
                                    // repeat this index if there are more times to run
                                    if (MyResults.Inputs[InputIndex].RemainingRuns > 0)
                                        SubNodeAnswers.Add(OptimizeRecursion(MyResults, InputIndex, IIUCopy, IRUCopy, CRCopy, LTCPDCopy, CurrentDepth + 1));                                        
                                    else
                                    {
                                        MyResults.Inputs[InputIndex].Succeeded = true;
                                        SubNodeAnswers.Add(OptimizeRecursion(MyResults, InputIndex + 1, IIUCopy, IRUCopy, CRCopy, LTCPDCopy, CurrentDepth + 1));
                                    }
                                    // Predict if a better answer is even possible from here
                                    // First see if adding every single remaining class for the remainder of this branch would 
                                    // create a result with more successful resutls than the best
                                    if (NodesPerDepth.Length - CurrentDepth + IncomingResults.Results.Count < CurrentBestAnswer.Results.Count)
                                    {
                                        switch (MyPriority)
                                        {
                                            case Priority.MaximizeSpecializedInstructors:
                                                if (IncomingResults.OptimizationScore >= CurrentBestAnswer.OptimizationScore)
                                                    return CurrentBestAnswer;
                                                break;
                                            case Priority.MinimizeForeignInstructorCount:
                                                if (IncomingResults.OptimizationScore >= CurrentBestAnswer.OptimizationScore)
                                                    return CurrentBestAnswer;
                                                break;
                                            case Priority.MinimizeInstructorTravelDistance:
                                                if (IncomingResults.OptimizationScore >= CurrentBestAnswer.OptimizationScore)
                                                    return CurrentBestAnswer;
                                                break;
                                            default:
                                                if (NodesPerDepth.Length - CurrentDepth + IncomingResults.Results.Count <=
                                                    CurrentBestAnswer.Results.Count)
                                                    return CurrentBestAnswer;
                                                break;
                                        }
                                    }
                                }

                                // Always check if the best answer is already found
                                if (IsABestAnswerFound())
                                    return CurrentBestAnswer;                          
                            }
                        }
                    }
                }

                // Must always consider what would happen if this input is not scheduled
                // Deep copy the current results so this child node will have a unique result object to build from
                var ResultsNotScheduled = OptimizerUtilities.DeepClone(IncomingResults);
                // Copy all the unavailability data trackers to update them for this recursion
                var CRCopy_skip = OptimizerUtilities.DeepClone(CurrentlyReleased);
                var IIUCopy_skip = OptimizerUtilities.DeepClone(IsInstructorUnavailable);
                var IRUCopy_skip = OptimizerUtilities.DeepClone(IsRoomUnavailable);
                var LTCPDCopy_skip = OptimizerUtilities.DeepClone(LocallyTaughtCoursesPerDay);

                // Always consider not scheduling this course
                //If there is a reason to skip, set it
                if (NoValidStartDates)
                    ResultsNotScheduled.Inputs[InputIndex].Reason = reason;
                else if (NoInstructor)
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "No instructor is available";
                else if (NoRoom)
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "No room is available";
                else if (CurrentInput.RemainingRuns > 0)
                {
                    ResultsNotScheduled.Inputs[InputIndex].Reason = $"Only scheduled {CurrentInput.NumTimesToRun - CurrentInput.RemainingRuns}" +
                        $" out of {CurrentInput.NumTimesToRun}";
                    ResultsNotScheduled.Inputs[InputIndex].NumTimesToRun = CurrentInput.RemainingRuns;
                }
                else
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "Skipped";

                // Recursion on skipping this input
                SubNodeAnswers.Add(OptimizeRecursion(ResultsNotScheduled, InputIndex + 1, IIUCopy_skip, IRUCopy_skip,
                    CRCopy_skip, LTCPDCopy_skip, CurrentDepth + CurrentInput.RemainingRuns));


                // Always check if the best answer is already found
                if (IsABestAnswerFound())
                {
                    return CurrentBestAnswer;
                }

                // Pick best answer
                var bestAnswer = SelectBestAnswer(SubNodeAnswers);
                NonEndNodesThatReturned++;

                return bestAnswer;
            }   
        }

        internal OptimizerScheduleResults OptimizeLongestToTeach(OptimizerScheduleResults IncomingResults, int InputIndex,
            Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable, Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable,
            Dictionary<int, Dictionary<string, int>> CurrentlyReleased,
            Dictionary<int, Dictionary<string, List<int>>> LocallyTaughtCoursesPerDay,
            int CurrentDepth, List<Course> Catalog)
        {
            // Always check if the best answer is already found
            if (IsABestAnswerFound())
            {
                return CurrentBestAnswer;
            }

            // Predict if a better answer is even possible from here
            // First see if adding every single remaining class for the remainder of this branch would 
            // create a result with more successful results than the best
            if (NodesPerDepth.Length - CurrentDepth + IncomingResults.Results.Count < CurrentBestAnswer.Results.Count)
            {
                if (IncomingResults.OptimizationScore >= CurrentBestAnswer.OptimizationScore)
                    return CurrentBestAnswer;
            }

            // base case if there are no more inputs
            if (InputIndex >= InputCount)
            {
                // calculate score
                CalculateScore(IncomingResults);
                // this answer has all its values, return
                TotalSchedulesCreated++;
                return IncomingResults;
            }
            // recursion
            else
            {
                // Obtain the current input information for reference
                var CurrentInput = IncomingResults.Inputs[InputIndex];

                // Container to store all the possible ways to schedule this input
                var PossibleSchedulingsForInput = new List<OptimizerResult>();

                // flags to determine possible reason for failure
                bool NoInstructor = true, NoRoom = true, NoValidStartDates = true;

                // Obtain the class max size and the location release rate for the function call to find the valid start dates
                var MaxClassSize = CourseCatalog.Where(course => course.ID == CurrentInput.CourseId).First().MaxSize;
                var ReleaseRate = Locations.Where(location => location.ID == CurrentInput.LocationIdLiteral).First().ReleaseRate;

                // Obtain the information about this course in the catalog
                var CourseInfo = Catalog.Where(course => course.ID == CurrentInput.CourseId).First();

                // Obtain all possible start dates (restricted by location release rate and course length)
                var reason = "";
                var ValidStartDates = FindValidStartDates(CurrentInput.LocationIdLiteral, CurrentInput.LengthDays, MaxClassSize, ReleaseRate,
                    (int)CourseInfo.ID, ref CurrentlyReleased, ref LocallyTaughtCoursesPerDay, ref reason);

                // Container to hold every child node's answer
                var SubNodeAnswers = new List<OptimizerScheduleResults>();

                // Loop through each day within the optimizer range that the course could start on
                var lastDate = ValidStartDates.LastOrDefault();

                foreach (var ValidStartDate in ValidStartDates)
                {
                    NoValidStartDates = false;
                    // Set the end date for this range based off of the course length
                    var ValidEndDate = Utilities.getNextBusinessDate(ValidStartDate, CurrentInput.LengthDays - 1);

                    // Sort the instructors by the last time they taught this course, with the longest taught being first
                    var SortedQualifiedInstructors = CourseInfo.QualifiedInstructors.OrderBy(i => i.Value).ToList();
                    
                    // Loop through all qualified instructors for this course
                    foreach (var Instructor in SortedQualifiedInstructors)
                    {
                        // Determine if this instructor is available for the range
                        if (IsInstructorAvailableForDateRange(Instructor.Key, ValidStartDate, ValidEndDate, IsInstructorUnavailable))
                        {
                            NoInstructor = false;

                            // Loop through all local rooms for this location
                            // but only the rooms that have the right type and quantity of resources required by this course type
                            foreach (var RoomID in Locations.Where(Loc => Loc.Code == CurrentInput.LocationID).First().
                                LocalRooms.Where(room => CourseInfo.RequiredResources.All(required =>
                                Rooms[room].Resources_dict.ContainsKey(required.Key) && Rooms[room].Resources_dict[required.Key] >= required.Value)))
                            {
                                // Determine if this room is available 
                                if (IsRoomAvailbleForDateRange(RoomID, ValidStartDate, ValidEndDate, IsRoomUnavailable))
                                {
                                    NoRoom = false;

                                    // Found an answer so set the remaining fields for the result
                                    // result object for this input
                                    var Result = new OptimizerResult
                                    {
                                        CourseID = CurrentInput.CourseId,
                                        LocationID = Locations.Where(Loc => Loc.Code == CurrentInput.LocationID).First().ID,
                                        Cancelled = false,
                                        StartTime = CurrentInput.StartTime,
                                        EndTime = CurrentInput.StartTime.Add(new TimeSpan(Math.Min(8, CourseInfo.Hours), 0, 0)),
                                        StartDate = ValidStartDate,
                                        EndDate = ValidEndDate,
                                        RequestType = "Optimizer",
                                        Requester = "Optimizer",
                                        Hidden = true,
                                        AttendanceLocked = false,
                                        Location = CurrentInput.LocationID,
                                        CourseCode = CurrentInput.CourseCode,
                                        RoomID = RoomID,
                                        InstrUsername = Instructor.Key,
                                        UsingLocalInstructor = Instructors.Where(instr => instr.Username == Instructor.Key).First().PointID == CurrentInput.LocationIdLiteral,
                                        inputID = CurrentInput.Id,
                                        LastTimeTaughtByInstructor = Catalog.First(c => c.ID == CurrentInput.CourseId).QualifiedInstructors[Instructor.Key]
                                    };

                                    NodesPerDepth[CurrentDepth] += 1;

                                    // Deep copy the current results so this child node will have a unique result object to build from
                                    var MyResults = OptimizerUtilities.DeepClone(IncomingResults);
                                    // Copy all the unavailability data trackers to update them for this recursion
                                    var CRCopy = OptimizerUtilities.DeepClone(CurrentlyReleased);
                                    var IIUCopy = OptimizerUtilities.DeepClone(IsInstructorUnavailable);
                                    var IRUCopy = OptimizerUtilities.DeepClone(IsRoomUnavailable);
                                    var LTCPDCopy = OptimizerUtilities.DeepClone(LocallyTaughtCoursesPerDay);
                                    var CatalogCopy = OptimizerUtilities.DeepClone(Catalog);


                                    //update the data container copies with the scheduling info for this result
                                    UpdateMatrices(Result.StartDate, Result.EndDate, CurrentInput.LocationIdLiteral, MaxClassSize, Result.InstrUsername, Result.RoomID, Result.UsingLocalInstructor,
                                        (int)CourseInfo.ID, ref CRCopy, ref IIUCopy, ref IRUCopy, ref LTCPDCopy);

                                    // Update the instructor in the course catalog so that the last time they taught the course is the last day of the result
                                    CatalogCopy.First(c => c.ID == CurrentInput.CourseId).QualifiedInstructors[Instructor.Key] = Result.EndDate;

                                    // Add the result
                                    MyResults.Results.Add(Result);
                                    MyResults.Inputs[InputIndex].RemainingRuns -= 1;

                                    // Add the child's answer 
                                    // repeat this index if there are more times to run
                                    if (MyResults.Inputs[InputIndex].RemainingRuns > 0)
                                        SubNodeAnswers.Add(OptimizeLongestToTeach(MyResults, InputIndex, IIUCopy, IRUCopy, CRCopy, LTCPDCopy, CurrentDepth + 1, CatalogCopy));
                                    else
                                    {
                                        MyResults.Inputs[InputIndex].Succeeded = true;
                                        SubNodeAnswers.Add(OptimizeLongestToTeach(MyResults, InputIndex + 1, IIUCopy, IRUCopy, CRCopy, LTCPDCopy, CurrentDepth + 1, CatalogCopy));
                                    }
                                    // Predict if a better answer is even possible from here
                                    // First see if adding every single remaining class for the remainder of this branch would 
                                    // create a result with more successful resutls than the best
                                    if (NodesPerDepth.Length - CurrentDepth + IncomingResults.Results.Count < CurrentBestAnswer.Results.Count)
                                    {
                                        if (IncomingResults.OptimizationScore >= CurrentBestAnswer.OptimizationScore)
                                            return CurrentBestAnswer;
                                    }
                                }

                                // Always check if the best answer is already found
                                if (IsABestAnswerFound())
                                    return CurrentBestAnswer;
                            }
                        }
                    }
                }

                // Must always consider what would happen if this input is not scheduled
                // Deep copy the current results so this child node will have a unique result object to build from
                var ResultsNotScheduled = OptimizerUtilities.DeepClone(IncomingResults);
                // Copy all the unavailability data trackers to update them for this recursion
                var CRCopy_skip = OptimizerUtilities.DeepClone(CurrentlyReleased);
                var IIUCopy_skip = OptimizerUtilities.DeepClone(IsInstructorUnavailable);
                var IRUCopy_skip = OptimizerUtilities.DeepClone(IsRoomUnavailable);
                var LTCPDCopy_skip = OptimizerUtilities.DeepClone(LocallyTaughtCoursesPerDay);
                var CatalogCopy_skip = OptimizerUtilities.DeepClone(Catalog);

                // Always consider not scheduling this course
                //If there is a reason to skip, set it
                if (NoValidStartDates)
                    ResultsNotScheduled.Inputs[InputIndex].Reason = reason;
                else if (NoInstructor)
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "No instructor is available";
                else if (NoRoom)
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "No room is available";
                else if (CurrentInput.RemainingRuns > 0)
                {
                    ResultsNotScheduled.Inputs[InputIndex].Reason = $"Only scheduled {CurrentInput.NumTimesToRun - CurrentInput.RemainingRuns}" +
                        $" out of {CurrentInput.NumTimesToRun}";
                    ResultsNotScheduled.Inputs[InputIndex].NumTimesToRun = CurrentInput.RemainingRuns;
                }
                else
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "Skipped";

                // Recursion on skipping this input
                SubNodeAnswers.Add(OptimizeLongestToTeach(ResultsNotScheduled, InputIndex + 1, IIUCopy_skip, IRUCopy_skip,
                    CRCopy_skip, LTCPDCopy_skip, CurrentDepth + CurrentInput.RemainingRuns, CatalogCopy_skip));


                // Always check if the best answer is already found
                if (IsABestAnswerFound())
                {
                    return CurrentBestAnswer;
                }

                // Pick best answer
                var bestAnswer = SelectBestAnswer(SubNodeAnswers);
                NonEndNodesThatReturned++;

                return bestAnswer;
            }
        }

        internal void PrimeStartingResults(ref Dictionary<int, Dictionary<string, bool>> isRoomUnavailable, 
            ref Dictionary<string, Dictionary<string, bool>> isInstructorUnavailable, ref Dictionary<int, Dictionary<string, int>> currentlyReleased,
            ref Dictionary<int, Dictionary<string, List<int>>> locallyTaughtCoursesPerDay)
        {
            // Loop through each input from the optimizer
            foreach (var CurrentInput in Inputs)
            {
                if (ShowDebugMessages) Console.Write($"Checking possibility for input {CurrentInput.Id}... ");

                // Obtain the class max size and the location release rate for the function call to find the valid start dates
                var MaxClassSize = CourseCatalog.Where(course => course.ID == CurrentInput.CourseId).First().MaxSize;
                var ReleaseRate = Locations.Where(location => location.ID == CurrentInput.LocationIdLiteral).First().ReleaseRate;

                // Obtain the information about this course in the catalog
                var CourseInfo = CourseCatalog.Where(course => course.ID == CurrentInput.CourseId).First();

                // Obtain all possible start dates (restricted by location release rate and course length)
                var reason = "";
                var ValidStartDates = FindValidStartDates(CurrentInput.LocationIdLiteral, CurrentInput.LengthDays, MaxClassSize, ReleaseRate,
                    (int)CourseInfo.ID, ref currentlyReleased, ref locallyTaughtCoursesPerDay, ref reason);
                if (ValidStartDates.Count <= 0)
                {
                    CurrentInput.Reason = reason;
                    WillAlwaysFail.Add(CurrentInput);
                    if (ShowDebugMessages) Console.WriteLine($"Impossible: {reason}");
                }
                else
                {
                    // Determine how many possible iterations could be fit into this date range for this course
                    // Used for bounding
                    var NonOverLappingStartDates = 0;
                    var CurrentDate = ValidStartDates.First();
                    while (CurrentDate <= ValidStartDates.Last())
                    {
                        NonOverLappingStartDates++;
                        CurrentDate = Utilities.getNextBusinessDate(CurrentDate, CurrentInput.LengthDays);
                        if (NonOverLappingStartDates >= CurrentInput.NumTimesToRun)
                            break;
                    }
                    CurrentInput.MaxPossibleIterations = NonOverLappingStartDates;
                    CurrentInput.RemainingRuns = CurrentInput.MaxPossibleIterations;
                }

                // Loop through each day within the optimizer range that the course could start on
                var lastDate = ValidStartDates.LastOrDefault();
                foreach (var ValidStartDate in ValidStartDates)
                {
                    // Set the end date for this range based off of the course length
                    var ValidEndDate = Utilities.getNextBusinessDate(ValidStartDate, CurrentInput.LengthDays - 1);

                    // Loop through all qualified instructors for this course
                    bool InstructorIsAvailable = false;
                    foreach (var Instructor in CourseInfo.QualifiedInstructors)
                    {
                        //Console.WriteLine("")
                        // Determine if this instructor is available for the range
                        if (IsInstructorAvailableForDateRange(Instructor.Key, ValidStartDate, ValidEndDate, isInstructorUnavailable))
                        {
                            // If an instructor is available continue
                            InstructorIsAvailable = true;
                            break;
                        }
                    }
                    // If no instructor is available and there are no more valid start dates, this input will always fail
                    if (!InstructorIsAvailable && ValidStartDate == lastDate)
                    {
                        // This input can never be scheduled because no instructor is available without any other inputs being scheduled
                        CurrentInput.Reason = "No instructor is available.";
                        WillAlwaysFail.Add(CurrentInput);
                        if (ShowDebugMessages) Console.WriteLine($"Impossible: {CurrentInput.Reason}");
                        break;
                    }

                    // Loop through all local rooms for this location
                    // but only the rooms that have the right type and quantity of resources required by this course type
                    var RoomIsAvailable = false;

                    foreach(var RoomID in Locations.Where(Loc => Loc.Code == CurrentInput.LocationID).First().
                        LocalRooms.Where(room => CourseInfo.RequiredResources.All(required =>
                        Rooms[room].Resources_dict.ContainsKey(required.Key) && Rooms[room].Resources_dict[required.Key] != null ?
                        Rooms[room].Resources_dict[required.Key] >= required.Value : true)))
                    {
                        Console.WriteLine(RoomID);
                        // Determine if this room is available 
                        if (IsRoomAvailbleForDateRange(RoomID, ValidStartDate, ValidEndDate, isRoomUnavailable))
                        {
                            RoomIsAvailable = true;
                            break;
                        }
                    }
                    // If no room is available and there are no more valid start dates, this input will always fail
                    if (!RoomIsAvailable && ValidStartDate == lastDate)
                    {
                        // This input can never be scheduled because no instructor is available without any other inputs being scheduled
                        CurrentInput.Reason = "No local room is available.";
                        WillAlwaysFail.Add(CurrentInput);
                        if (ShowDebugMessages) Console.WriteLine($"Impossible: {CurrentInput.Reason}");
                        break;
                    }
                    if (InstructorIsAvailable && RoomIsAvailable)
                    {
                        if (ShowDebugMessages) Console.WriteLine("Possible");
                        break;
                    }
                }
            }
            // Remove the objects that will always fail from the input
            WillAlwaysFail.ForEach(x => Inputs.Remove(x));

            // Sort Inputs by Remaining inputs
            Inputs = Inputs.OrderByDescending(x => x.MaxPossibleIterations).ToList();

        }

        private void CalculateScore(OptimizerScheduleResults incomingResults)
        {
            // Set the score for these results based off of the prioritization
            switch (MyPriority)
            {
                case Priority.MaximizeInstructorLongestToTeach:
                    var totalDaysBetweenLastAssignment = 0;
                    foreach(var x in incomingResults.Results)
                    {
                        totalDaysBetweenLastAssignment += (x.StartDate.Date - x.LastTimeTaughtByInstructor.Date).Days;
                    }
                    incomingResults.OptimizationScore = totalDaysBetweenLastAssignment;
                    break;
                case (Priority.MaximizeSpecializedInstructors):
                    var totalInstructorQualifications = 0;
                    foreach(var x in incomingResults.Results)
                    {
                        totalInstructorQualifications += Instructors.First(y => x.InstrUsername == y.Username).QualificationCount;
                    }
                    incomingResults.OptimizationScore = totalInstructorQualifications;
                    break;
                case (Priority.MinimizeForeignInstructorCount):
                    incomingResults.OptimizationScore = incomingResults.Results.Where(x => !x.UsingLocalInstructor).ToList().Count;
                    break;
                case (Priority.MinimizeInstructorTravelDistance):
                    double totalDistanceTraveled = 0;
                    foreach(var x in incomingResults.Results)
                    { 
                        var clasLoc = Locations.First(y => y.ID == x.LocationID);
                        var instrLoc = Locations.First(y => y.ID == Instructors.First(z => z.Username == x.InstrUsername).PointID);
                        totalDistanceTraveled += Utilities.getDistanceLatLong(clasLoc.Latitude, clasLoc.Longitude, instrLoc.Latitude,
                            instrLoc.Longitude);{ }
                    }
                    incomingResults.OptimizationScore = (int)totalDistanceTraveled;
                    break;
                default:
                    incomingResults.OptimizationScore = incomingResults.Results.Count;
                    break;
            }
            // If the results have as many successful results, see if there is a better score and then set the best if so
            if (incomingResults.Results.Count == CurrentBestAnswer.Results.Count)
            {
                switch (MyPriority)
                {
                    case Priority.MaximizeInstructorLongestToTeach:
                        if (incomingResults.OptimizationScore > CurrentBestAnswer.OptimizationScore)
                            CurrentBestAnswer = incomingResults;
                        break;
                    case (Priority.MaximizeSpecializedInstructors):
                        if (incomingResults.OptimizationScore < CurrentBestAnswer.OptimizationScore)
                            CurrentBestAnswer = incomingResults;
                        break;
                    case (Priority.MinimizeForeignInstructorCount):
                        if (incomingResults.OptimizationScore < CurrentBestAnswer.OptimizationScore)
                            CurrentBestAnswer = incomingResults;
                        break;
                    case (Priority.MinimizeInstructorTravelDistance):
                        if (incomingResults.OptimizationScore < CurrentBestAnswer.OptimizationScore)
                            CurrentBestAnswer = incomingResults;
                        break;
                    default:
                        break;
                }
            }
            // if the resutls had more successful results, it is automatically the new best
            if (incomingResults.Results.Count > CurrentBestAnswer.Results.Count)
                CurrentBestAnswer = incomingResults;

            // check if the answer is now the best possible
            if (CurrentBestAnswer.Results.Count >= NodesPerDepth.Length)
            {
                switch (MyPriority)
                {
                    case Priority.MaximizeInstructorLongestToTeach:
                        if (incomingResults.OptimizationScore >= BestPossibleScore)
                            ABestAnswerFound = true;
                        break;
                    case (Priority.MaximizeSpecializedInstructors):
                        if (incomingResults.OptimizationScore <= BestPossibleScore)
                            ABestAnswerFound = true;
                        break;
                    case (Priority.MinimizeForeignInstructorCount):
                        if (incomingResults.OptimizationScore <= BestPossibleScore)
                            ABestAnswerFound = true;
                        break;
                    case (Priority.MinimizeInstructorTravelDistance):
                        if (incomingResults.OptimizationScore <= BestPossibleScore)
                            ABestAnswerFound = true;
                        break;
                    default:
                        ABestAnswerFound = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Return a status report of the current optimization
        /// </summary>
        public string GetStatus(string counter, TimeSpan elapsedTime)
        {
            var status = $"\n              Status {counter} \n";
            status += "--------------------------------------|\n";
            status += $"Elapsed time: {elapsedTime.Hours}h:{elapsedTime.Minutes}m:{elapsedTime.Seconds}s\n";
            status += $"Total schedules created: {TotalSchedulesCreated}\n";
            status += $"Non end-node evaluations completed: {NonEndNodesThatReturned}\n";
            status += $"Current best optimization score: {CurrentBestAnswer.OptimizationScore}\n";
            status += $"Current best optimization answer result size: {CurrentBestAnswer.Results.Count}\n";
            status += $"Best possible score: {BestPossibleScore}\n";
            status += "Nodes per tree level\n";
            status += "Level 0: Node Count 1\n";
            for (int i = 0; i < NodesPerDepth.Length; i++)
            {
                if (NodesPerDepth[i] == 0)
                    status += $"Level {i + 1}: Node Count ?\n";
                else
                    status += $"Level {i + 1}: Node Count {NodesPerDepth[i]}\n";
            }
            status += "--------------------------------------|";
            return status;
        }

        public bool IsABestAnswerFound()
        {
            return ABestAnswerFound;
        }

        private OptimizerScheduleResults SelectBestAnswer(List<OptimizerScheduleResults> subNodeAnswers)
        {
            var MostSuccessfullyScheduled = subNodeAnswers.Max( x => x.Results.Count);
            var AnswersWithHighestSuccessfullyScheduled = subNodeAnswers.Where(x => x.Results.Count == MostSuccessfullyScheduled);

            switch (MyPriority)
            {
                case Priority.MaximizeInstructorLongestToTeach:
                    return AnswersWithHighestSuccessfullyScheduled.MaxBy(x => x.OptimizationScore).First();
                case (Priority.MaximizeSpecializedInstructors):
                    return AnswersWithHighestSuccessfullyScheduled.MinBy(x => x.OptimizationScore).First();
                case (Priority.MinimizeForeignInstructorCount):
                    return AnswersWithHighestSuccessfullyScheduled.MinBy(x => x.OptimizationScore).First();
                case (Priority.MinimizeInstructorTravelDistance):
                    return AnswersWithHighestSuccessfullyScheduled.MinBy(x => x.OptimizationScore).First();
                default:
                    return AnswersWithHighestSuccessfullyScheduled.First();
            }
        }

        /// <summary>
        /// Determines the possible days a course could start on based on the release rate of its location and concurrent courses
        /// </summary>
        /// <param name="locationId">The location the class is to be scheduled at</param>
        /// <param name="courseId">The id of the course type being scheduled</param>
        /// <returns>A collection of valid start days for this class</returns>
        private List<DateTime> FindValidStartDates(int locationId, int classLengthDays, int classMaxSize, int? releaseRate,
            int courseId, ref Dictionary<int, Dictionary<string, int>> CurrentlyReleased, 
            ref Dictionary<int, Dictionary<string, List<int>>> LocallyTaughtCoursesPerDay, ref string reason)
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
                    if (CurrentlyReleased[locationId][CurrentDay.ToString(TIME_FORMAT)] + classMaxSize > releaseRate)
                    {
                        // Set how many days to skip so that the foreach starts on the day after this one
                        // Example, we started checking from a Monday for a 5 day class, and Wednesday won't work, so we will set DaysToSkip to 2
                        // and we will skip tuesday and wednesay and start on thursday again which is the next day that the course can be scheduled
                        DaysToSkip = (int)(CurrentDay - FirstDay).TotalDays;
                        ReleaseRateSatisfied = false;
                        reason = $"The release rate at {Locations.First(x => x.ID == locationId).Code} would have been exceeded";
                        break;
                    }

                    // If a course with the same code is being taught at this location, then do not include it
                    // This is to avoid having two of the same course at the same time
                    if (LocallyTaughtCoursesPerDay[locationId][CurrentDay.ToString(TIME_FORMAT)].Contains(courseId))
                    {
                        DaysToSkip = (int)(CurrentDay - FirstDay).TotalDays;
                        ReleaseRateSatisfied = false;
                        reason = $"This class is already being taught at {Locations.First(x => x.ID == locationId).Code} during the date range";
                        break;
                    }
                }
                // Add this day to valid start dates if the whole range was satisfied
                if (ReleaseRateSatisfied)
                {
                    ValidStartDates.Add(FirstDay);
                }
            }
            if (reason == "")
            {
                reason = $"The class is {classLengthDays} days long while the date range is {TotalWeekDays} days long";
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
        private bool IsInstructorAvailableForDateRange(string instructor, DateTime startDate, DateTime endDate, Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable)
        {
            // Loop through all days in the current course range
            foreach (var CurrentDay in OptimizerUtilities.EachWeekDay(startDate, endDate))
            {
                // If instructor is unavailable for any day, return false
                if (IsInstructorUnavailable[instructor][ CurrentDay.ToString(TIME_FORMAT)])
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
        private bool IsRoomAvailbleForDateRange(int roomID, DateTime startDate, DateTime endDate, Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable)
        {
            // Loop through all the days in the current date range
            foreach(var CurrentDay in OptimizerUtilities.EachWeekDay(startDate, endDate))
            {
                // If room is unavailable for any day, return false
                if (IsRoomUnavailable[roomID][CurrentDay.ToString(TIME_FORMAT)])
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
        private void UpdateMatrices(DateTime validStartDate, DateTime validEndDate, int locationId, int maxClassSize, string instrUsername,
            int roomID, bool localAssignment, int courseID, ref Dictionary<int, Dictionary<string, int>> CurrentlyReleased,
            ref Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable, ref Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable,
            ref Dictionary<int, Dictionary<string, List<int>>> LocallyTaughtCoursesPerDay)
        {
            // Go through each day the course will take place
            foreach(var currentDay in OptimizerUtilities.EachWeekDay(validStartDate, validEndDate))
            {
                var currentDayString = currentDay.ToString(TIME_FORMAT);
                CurrentlyReleased[locationId][currentDayString] += maxClassSize;
                IsInstructorUnavailable[instrUsername][currentDayString] = true;
                IsRoomUnavailable[roomID][currentDayString] = true;
                LocallyTaughtCoursesPerDay[locationId][currentDayString].Add(courseID);
            }

            if (!localAssignment)
            {
                // Check if the day before the start day for the classs is in the index map
                // If its not, it is either an adjacent week day but outside the range of the optimizer
                // Or it is a weekend (the day before monday is sunday, will not be mapped in the dictionary
                var DayBeforeAssignment = OptimizerUtilities.Max(validStartDate, StartDate).AddDays(-1).ToString(TIME_FORMAT);
                if (IsInstructorUnavailable.First().Value.ContainsKey(DayBeforeAssignment))
                {
                    IsInstructorUnavailable[instrUsername][DayBeforeAssignment] = true;
                }
                // Do same but for the day after
                var DayAfterAssignment = OptimizerUtilities.Min(validEndDate, EndDate).ToString(TIME_FORMAT);
                if (IsInstructorUnavailable.First().Value.ContainsKey(DayAfterAssignment))
                {
                    IsInstructorUnavailable[instrUsername][DayAfterAssignment] = true;
                }
            }
        }

        private static double GetBusinessDays(DateTime startD, DateTime endD)
        {
            double calcBusinessDays =
                1 + ((endD - startD).TotalDays * 5 -
                (startD.DayOfWeek - endD.DayOfWeek) * 2) / 7;

            if (endD.DayOfWeek == DayOfWeek.Saturday) calcBusinessDays--;
            if (startD.DayOfWeek == DayOfWeek.Sunday) calcBusinessDays--;

            return calcBusinessDays;
        }
    }
}
