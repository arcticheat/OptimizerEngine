using ConsoleTables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OptimizerEngine.Models
{
    [Serializable]
    public struct OptimizerScheduleResults
    {
        public List<OptimizerResult> Results;
        public List<OptimizerInput> Inputs;
        public int OptimizationScore;

        public OptimizerScheduleResults(OptimizerScheduleResults op)
        {
            this.Results = new List<OptimizerResult>(op.Results);
            this.Inputs = new List<OptimizerInput> (op.Inputs);
            this.OptimizationScore = op.OptimizationScore;
        }

        public void Print()
        {
            Console.WriteLine("Optimizer Successful Results");
            ConsoleTable.From<OptimizerResultPrintable>(Results.Select(result => new OptimizerResultPrintable(result))).Write(Format.MarkDown);
            Console.WriteLine("");

            Console.WriteLine("Optimizer Failed Results");
            ConsoleTable.From<OptimizerInput>(Inputs.Where(input => input.Succeeded == false)).Write(Format.MarkDown);
            Console.WriteLine();
        }
    }
}
