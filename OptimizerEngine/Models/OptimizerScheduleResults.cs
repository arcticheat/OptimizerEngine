using ConsoleTables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OptimizerEngine.Models
{
    public class OptimizerScheduleResults
    {
        public List<OptimizerResult> Results = new List<OptimizerResult>();
        public List<OptimizerInput> FailedToSchedule = new List<OptimizerInput>();
        public int OptimizationScore = -1;

        public OptimizerScheduleResults()
        {

        }
        public OptimizerScheduleResults(OptimizerScheduleResults op)
        {
            this.Results = new List<OptimizerResult>(op.Results);
            this.FailedToSchedule = new List<OptimizerInput> (op.FailedToSchedule);
            this.OptimizationScore = op.OptimizationScore;
        }

        public void Print()
        {
            Console.WriteLine("Optimizer Successful Results");
            ConsoleTable.From<OptimizerResultPrintable>(Results.Select(result => new OptimizerResultPrintable(result))).Write(Format.MarkDown);
            Console.WriteLine("");

            Console.WriteLine("Optimizer Failed Results");
            ConsoleTable.From<OptimizerInput>(FailedToSchedule).Write(Format.MarkDown);
            Console.WriteLine();
        }
    }
}
