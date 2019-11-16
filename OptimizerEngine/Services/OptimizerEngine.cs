﻿using ConsoleTables;
using OptimizerEngine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text;
using MoreLinq;

namespace OptimizerEngine.Services
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
        public string TIME_FORMAT = "MM/dd/yyyy";
        public DateTime StartDate;
        public DateTime EndDate;
        public bool ShowDebugMessages;
        public int InputCount;
        private int CurrentBestScore = -1;
        private bool FullAnswerFound = false;
        public int TotalWeekDays;

        /// <summary>
        /// The optimizer will calculate a single collection of optimizer results created from the optimizer input
        /// The results are created by ensuring the schedule is legal as well as assigning the resources on a first available focus
        /// </summary>
        public void OptimizeGreedy(Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable, Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable,
            Dictionary<int, Dictionary<string, int>> CurrentlyReleased)
        {
            if (ShowDebugMessages) Console.WriteLine("Optimizing...\n");
            // Container for the results
            var Results = new List<OptimizerResult>();

            // Loop through each input from the optimizer
            foreach (var CurrentInput in Inputs)
            {
                if (ShowDebugMessages) Console.WriteLine($"Calculating result for input ID {CurrentInput.Id}... ");

                // Obtain the class max size and the location release rate for the function call to find the valid start dates
                var MaxClassSize = CourseCatalog.Where(course => course.Id == CurrentInput.CourseId).First().MaxSize;
                var ReleaseRate = Locations.Where(location => location.Id == CurrentInput.LocationIdLiteral).First().ReleaseRate;

                // Obtain the information about this course in the catalog
                var CourseInfo = CourseCatalog.Where(course => course.Id == CurrentInput.CourseId).First();

                // Obtain all possible start dates (restricted by location release rate and course length)
                var ValidStartDates = FindValidStartDates(CurrentInput.LocationIdLiteral, CurrentInput.LengthDays, MaxClassSize, ReleaseRate, CurrentlyReleased);
                if (ValidStartDates.Count <= 0)
                {
                    if (ShowDebugMessages) Console.WriteLine($"The input could not be scheduled because the location {CurrentInput.LocationId} would exceed its release rate.\n");
                    CurrentInput.Reason = "Release rate would be exceeded.";
                }

                // Counter to keep track of inputs that need multiple iterations scheduled 
                var currentIterationForThisInput = 0;

                // Loop through each day within the optimizer range that the course could start on
                var lastDate = ValidStartDates.LastOrDefault();
                
                foreach (var ValidStartDate in ValidStartDates)
                {
                    // result object for this input
                    var Result = new OptimizerResult
                    {
                        InstrUsername = "",
                        RoomID = 0
                    };

                    if (ShowDebugMessages) Console.WriteLine($"Searching on the start date {ValidStartDate.ToString(TIME_FORMAT)} for an instructor and room.");
                    // Set the end date for this range based off of the course length
                    var ValidEndDate = ValidStartDate.AddDays(CurrentInput.LengthDays - 1);

                    // Loop through all qualified instructors for this course
                    foreach (var Instructor in CourseInfo.QualifiedInstructors)
                    {
                        // Determine if this instructor is available for the range
                        if (IsInstructorAvailableForDateRange(Instructor, ValidStartDate, ValidEndDate, IsInstructorUnavailable))
                        {
                            if (ShowDebugMessages) Console.WriteLine($"The instructor {Instructor} is available.");
                            Result.InstrUsername = Instructor;
                            // Set the result status to using a local instructor
                            Result.UsingLocalInstructor = Instructors.Where(instr => instr.Username == Instructor).First().PointId == CurrentInput.LocationIdLiteral;
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
                    foreach (var RoomID in Locations.Where(Loc => Loc.Code == CurrentInput.LocationId).First().
                        LocalRooms.Where(room => CourseInfo.RequiredResources.All(required =>
                        Rooms[room].Resources.ContainsKey(required.Key) && Rooms[room].Resources[required.Key] >= required.Value )))
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
                        ref CurrentlyReleased, ref IsInstructorUnavailable, ref IsRoomUnavailable);

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
                    Result.AttendanceLocked = false;
                    Result.Location = CurrentInput.LocationId;
                    Result.CourseCode = CurrentInput.CourseCode;

                    if (ShowDebugMessages)
                    {
                        Console.WriteLine($"The course will be scheduled from {ValidStartDate.ToString(TIME_FORMAT)}" +
                        $" to {ValidStartDate.AddDays(CurrentInput.LengthDays - 1).ToString(TIME_FORMAT)}  with the room and instructor listed above.");
                        Console.WriteLine();
                    }

                    // Add to the result container
                    Results.Add(Result);

                    // Keep running if the same course needs to be scheduled additional times
                    if (currentIterationForThisInput++ < CurrentInput.NumTimesToRun - 1)
                    {
                        if (ValidStartDate == lastDate)
                        {
                            CurrentInput.Reason = $"Could only schedule {currentIterationForThisInput} out of {CurrentInput.NumTimesToRun} requests";
                            if (ShowDebugMessages) Console.WriteLine("This input is required to be schedule again, but no more days are available.\n");
                        }
                        else
                            if (ShowDebugMessages) Console.WriteLine("This input is required to be scheduled again. Continuing from next valid start date...");
                    }
                    // Otherwise, continue to the next input
                    else
                    {
                        CurrentInput.Succeeded = true;
                        break;
                    }
                }
            }
            if (ShowDebugMessages)
            {
                Console.WriteLine("Optimizer Successful Results");
                ConsoleTable.From<OptimizerResultPrintable>(Results.Select(result => new OptimizerResultPrintable(result))).Write(Format.MarkDown);
                Console.WriteLine("");
            }

            if (Inputs.Count(input => !input.Succeeded) > 0 && ShowDebugMessages)
            {
                Console.WriteLine("Optimizer Failed Results");
                ConsoleTable.From<OptimizerInput>(Inputs.Where(input => !input.Succeeded)).Write(Format.MarkDown);
                Console.WriteLine();
            }

            using (var context = new DatabaseContext())
            {
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
            }

            if (ShowDebugMessages) Console.WriteLine("Optimization Complete.\n");
        }

        internal OptimizerScheduleResults OptimizeRecursion(OptimizerScheduleResults IncomingResults, int InputIndex,
            Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable, Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable,
            Dictionary<int, Dictionary<string, int>> CurrentlyReleased)
        {
            // base case if there are no more inputs
            if (InputIndex >= InputCount)
            {
                // calculate score
                IncomingResults.OptimizationScore = IncomingResults.Results.Count;
                // this answer has all the values, return
                if (IncomingResults.OptimizationScore == InputCount)
                    FullAnswerFound = true;

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
                bool NoInstructor = true, NoRoom = true, ExceededReleaseRate = true;

                // Obtain the class max size and the location release rate for the function call to find the valid start dates
                var MaxClassSize = CourseCatalog.Where(course => course.Id == CurrentInput.CourseId).First().MaxSize;
                var ReleaseRate = Locations.Where(location => location.Id == CurrentInput.LocationIdLiteral).First().ReleaseRate;

                // Obtain the information about this course in the catalog
                var CourseInfo = CourseCatalog.Where(course => course.Id == CurrentInput.CourseId).First();

                // Obtain all possible start dates (restricted by location release rate and course length)
                var ValidStartDates = FindValidStartDates(CurrentInput.LocationIdLiteral, CurrentInput.LengthDays, MaxClassSize, ReleaseRate, CurrentlyReleased);
                    

                // Loop through each day within the optimizer range that the course could start on
                var lastDate = ValidStartDates.LastOrDefault();

                foreach (var ValidStartDate in ValidStartDates)
                {
                    ExceededReleaseRate = false;
                    // Set the end date for this range based off of the course length
                    var ValidEndDate = ValidStartDate.AddDays(CurrentInput.LengthDays - 1);

                    // Loop through all qualified instructors for this course
                    foreach (var Instructor in CourseInfo.QualifiedInstructors)
                    {
                        // Determine if this instructor is available for the range
                        if (IsInstructorAvailableForDateRange(Instructor, ValidStartDate, ValidEndDate, IsInstructorUnavailable))
                        {
                            NoInstructor = false;

                            // Loop through all local rooms for this location
                            // but only the rooms that have the right type and quantity of resources required by this course type
                            foreach (var RoomID in Locations.Where(Loc => Loc.Code == CurrentInput.LocationId).First().
                                LocalRooms.Where(room => CourseInfo.RequiredResources.All(required =>
                                Rooms[room].Resources.ContainsKey(required.Key) && Rooms[room].Resources[required.Key] >= required.Value)))
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
                                        LocationID = Locations.Where(Loc => Loc.Code == CurrentInput.LocationId).First().Id,
                                        Cancelled = false,
                                        StartTime = ValidStartDate - ValidStartDate,
                                        EndTime = ValidStartDate - ValidStartDate,
                                        StartDate = ValidStartDate,
                                        EndDate = ValidStartDate.AddDays(CurrentInput.LengthDays - 1),
                                        RequestType = "Optimizer",
                                        Requester = "Optimizer",
                                        Hidden = true,
                                        AttendanceLocked = false,
                                        Location = CurrentInput.LocationId,
                                        CourseCode = CurrentInput.CourseCode,
                                        RoomID = RoomID,
                                        InstrUsername = Instructor,
                                        UsingLocalInstructor = Instructors.Where(instr => instr.Username == Instructor).First().PointId == CurrentInput.LocationIdLiteral
                                    };

                                    // Add this result to all the possible results from this node
                                    PossibleSchedulingsForInput.Add(Result);
                                }
                            }
                        }
                    }
                }
                // Container to hold every child node's answer
                var SubNodeAnswers = new List<OptimizerScheduleResults>();

                // If this course should be scheduled multiple times, obtain all combinations of this class with itself scheduled at different times
                var combos = BuildCombinations(PossibleSchedulingsForInput, new List<OptimizerResult>(), CurrentInput.NumTimesToRun, -1);
                // branch for each combo 
                foreach (var combo in combos)
                {
                    // Deep copy the current results so this child node will have a unique result object to build from
                    var MyResults = OptimizerUtilities.DeepClone(IncomingResults);

                    // Copy all the unavailability data trackers to update them for this recursion
                    var CRCopy = new Dictionary<int, Dictionary<string, int>>();
                    foreach (var outerPair in CurrentlyReleased)
                    {
                        CRCopy[outerPair.Key] = new Dictionary<string, int>();
                        foreach (var innerPair in outerPair.Value)
                        {
                            CRCopy[outerPair.Key][innerPair.Key] = innerPair.Value;
                        }
                    }
                    var IIUCopy = new Dictionary<string, Dictionary<string, bool>>();
                    foreach (var outerPair in IsInstructorUnavailable)
                    {
                        IIUCopy[outerPair.Key] = new Dictionary<string, bool>();
                        foreach (var innerPair in outerPair.Value)
                        {
                            IIUCopy[outerPair.Key][innerPair.Key] = innerPair.Value;
                        }
                    }
                    var IRUCopy = new Dictionary<int, Dictionary<string, bool>>();
                    foreach (var outerPair in IsRoomUnavailable)
                    {
                        IRUCopy[outerPair.Key] = new Dictionary<string, bool>();
                        foreach (var innerPair in outerPair.Value)
                        {
                            IRUCopy[outerPair.Key][innerPair.Key] = innerPair.Value;
                        }
                    }

                    // update the availability markers for each result of the combo and add it to the results
                    foreach (var Result in combo)
                    {
                        //update the data container copies with the scheduling info for this result
                        UpdateMatrices(Result.StartDate, Result.EndDate, CurrentInput.LocationIdLiteral, MaxClassSize, Result.InstrUsername, Result.RoomID, Result.UsingLocalInstructor,
                            ref CRCopy, ref IIUCopy, ref IRUCopy);

                        // Add the result
                        MyResults.Results.Add(Result);
                    }
                    // Set that the input has succeeded
                    if (combo.Count < CurrentInput.NumTimesToRun)
                    {
                        MyResults.Inputs[InputIndex].Succeeded = false;
                        MyResults.Inputs[InputIndex].Reason = $"Could only schedule {combo.Count} out of {CurrentInput.NumTimesToRun}.";
                    }
                    else
                        MyResults.Inputs[InputIndex].Succeeded = true;
                    // Add the child's answer 
                    var SubNodeAnswer = new OptimizerScheduleResults(OptimizeRecursion(MyResults, InputIndex + 1, IIUCopy, IRUCopy, CRCopy));
                    SubNodeAnswers.Add(SubNodeAnswer);
                }

                // Must always consider what would happen if this input is not scheduled
                var ResultsNotScheduled = OptimizerUtilities.DeepClone(IncomingResults);
                ResultsNotScheduled.Inputs[InputIndex].Succeeded = false;

                // Always consider not scheduling this course
                //If there is a reason to skip, set it
                if (ExceededReleaseRate)
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "No valid start date";
                if (NoInstructor)
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "No instructor is available";
                else if (NoRoom)
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "No room is available";
                else
                    ResultsNotScheduled.Inputs[InputIndex].Reason = "Skipped";

                // Copy all the unavailability data trackers to update them for this recursion
                var CRCopy_skip = new Dictionary<int, Dictionary<string, int>>();
                foreach (var outerPair in CurrentlyReleased)
                {
                    CRCopy_skip[outerPair.Key] = new Dictionary<string, int>();
                    foreach (var innerPair in outerPair.Value)
                    {
                        CRCopy_skip[outerPair.Key][innerPair.Key] = innerPair.Value;
                    }
                }
                var IIUCopy_skip = new Dictionary<string, Dictionary<string, bool>>();
                foreach (var outerPair in IsInstructorUnavailable)
                {
                    IIUCopy_skip[outerPair.Key] = new Dictionary<string, bool>();
                    foreach (var innerPair in outerPair.Value)
                    {
                        IIUCopy_skip[outerPair.Key][innerPair.Key] = innerPair.Value;
                    }
                }
                var IRUCopy_skip = new Dictionary<int, Dictionary<string, bool>>();
                foreach (var outerPair in IsRoomUnavailable)
                {
                    IRUCopy_skip[outerPair.Key] = new Dictionary<string, bool>();
                    foreach (var innerPair in outerPair.Value)
                    {
                        IRUCopy_skip[outerPair.Key][innerPair.Key] = innerPair.Value;
                    }
                }

                // Recursion on skipping this input
                var AnswerWithoutThisInput = OptimizeRecursion(ResultsNotScheduled, InputIndex + 1, IIUCopy_skip, IRUCopy_skip, CRCopy_skip);
                SubNodeAnswers.Add(AnswerWithoutThisInput);

                // Pick best answer
                var bestAnswer = SelectMostScheduled(SubNodeAnswers);
                //if (bestAnswer.OptimizationScore > CurrentBestScore)
                return bestAnswer;
            }   
        }

        /// <summary>
        /// Builds all possible combinations of optimizer results for an input that must run more than once
        /// </summary>
        /// <param name="possibleScheduling">Reference to the list of possible schedulings for this class input</param>
        /// <param name="myCombination">The combination of class schedulings that is being recursively being built</param>
        /// <param name="Index">The current index to check the list of possible scheduling</param>
        /// <param name="numTimesToRun">The total number of times the course should be scheduled, or, the length of the combination</param>
        /// <returns></returns>
        public List<List<OptimizerResult>> BuildCombinations(List<OptimizerResult> Entities, List<OptimizerResult> MyCombo, int NumTimesToRun, int IndexOfLastAdded)
        {
            var MyComboCollection = new List<List<OptimizerResult>>();

            if (Entities.Count <= 0)
                return MyComboCollection;

            // Base Case
            if (MyCombo.Count >= NumTimesToRun || MyCombo.Count >= Entities.Count || MyCombo.Count >= TotalWeekDays)
            {
                MyComboCollection.Add(MyCombo);
            }
            //Recursion 
            else
            {
                // Loop through every possible result after the last one
                for(var i = IndexOfLastAdded + 1; i < Entities.Count; i++)
                {
                    // If there are previous results in the combo to check for conflicts
                    if (MyCombo.Count > 0)
                    {
                        // Check every current result in the combo
                        foreach(var ResultInCombo in MyCombo)
                        {
                            // Make sure that the possible result isn't at the same time as an existing result of the combo 
                            if (ResultInCombo.StartDate != Entities[i].StartDate)
                            {
                                var MyComboCopy = OptimizerUtilities.DeepClone(MyCombo);
                                MyComboCopy.Add(Entities[i]);
                                MyComboCollection.AddRange(BuildCombinations(Entities, MyComboCopy, NumTimesToRun, i));
                            }
                        }
                    }
                    else
                    {
                        var MyComboCopy = OptimizerUtilities.DeepClone(MyCombo);
                        MyComboCopy.Add(Entities[i]);
                        MyComboCollection.AddRange(BuildCombinations(Entities, MyComboCopy, NumTimesToRun, i));
                    }
                }
            }
            return MyComboCollection;
        }

        private OptimizerScheduleResults SelectMostScheduled(List<OptimizerScheduleResults> subNodeAnswers)
        {
            var max = 0;
            var bestAnswer = new OptimizerScheduleResults
            {
                Results = new List<OptimizerResult>(),
                Inputs = new List<OptimizerInput>(),
                OptimizationScore = 0
            };
            foreach(var answer in subNodeAnswers)
            {
                if (answer.Results.Count == 0) continue;
                if (answer.Results.Count > max)
                {
                    max = answer.Results.Count;
                    bestAnswer = answer;
                }
            }
            bestAnswer.OptimizationScore = max;
            return bestAnswer;
        }

        /// <summary>
        /// Determines the possible days a course could start on based on the release rate of its location
        /// </summary>
        /// <param name="locationId">The location the class is to be scheduled at</param>
        /// <param name="courseId">The id of the course type being scheduled</param>
        /// <returns>A collection of valid start days for this class</returns>
        private List<DateTime> FindValidStartDates(int locationId, int classLengthDays, int classMaxSize, int? releaseRate, Dictionary<int, Dictionary<string, int>> CurrentlyReleased)
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
            int roomID, bool localAssignment, ref Dictionary<int, Dictionary<string, int>> CurrentlyReleased,
            ref Dictionary<string, Dictionary<string, bool>> IsInstructorUnavailable, ref Dictionary<int, Dictionary<string, bool>> IsRoomUnavailable)
        {
            // Go through each day the course will take place
            foreach(var currentDay in OptimizerUtilities.EachWeekDay(validStartDate, validEndDate))
            {
                var currentDayString = currentDay.ToString(TIME_FORMAT);
                CurrentlyReleased[locationId][currentDayString] += maxClassSize;
                IsInstructorUnavailable[instrUsername][currentDayString] = true;
                IsRoomUnavailable[roomID][currentDayString] = true;
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
    }
}
