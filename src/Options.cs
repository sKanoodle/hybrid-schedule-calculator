using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridScheduleCalculator
{
    class Options
    {
        [Option('a', "max-average-difference",
            Default = 2.0,
            HelpText = "Sets the maximum allowed average difference between number of students in week A and week B in all courses.")]
        public double MaxAvgDifference { get; set; }

        [Option('b', "max-absolute-difference",
            Default = 4,
            HelpText = "Sets the maximum allowed difference between week A and B that no course can exceed.")]
        public int MaxAbsDifference { get; set; }

        [Option('t', "thread-count",
            Default = 2,
            HelpText = "Sets number of threads the application will use. More threads means more calculations in the same amount of time.")]
        public int ThreadCount { get; set; }

        [Value(0,
            MetaName = "CSV File Path",
            HelpText = "Path to the csv that contains all the students and their courses.",
            Required = true)]
        public string Filepath { get; set; }

        [Option('g', "grade-filter",
            Default = -1,
            HelpText = "In case there is multiple grades in the csv, filter to calculate only a specific grade.")]
        public int GradeFilter { get; set; }
    }
}
