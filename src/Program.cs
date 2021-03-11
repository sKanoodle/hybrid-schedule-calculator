using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace HybridScheduleCalculator
{
    class Program
    {
        private static readonly Random MasterRandom = new Random();

        static void Main(string[] args)
        {
            decimal maxAvgDistance = decimal.Parse(args[0]);
            int maxAbsDistance = int.Parse(args[1]);
            string studentsFile = args[2];
            int grade = -1;
            if (args.Length > 3)
                grade = int.Parse(args[3]);

            Calculate(maxAvgDistance, maxAbsDistance, studentsFile, grade);

            Console.WriteLine("press any key to exit");
            Console.Read();
        }

        private static void Calculate(decimal maxAvgDistance, int maxAbsDistance, string studentsFile, int grade = -1)
        {
            IEnumerable<Student> students;
            const int threadCount = 8;
            bool abortRequested = false;

            object lockObject = new();
            int successCount = 0;
            int[] testedPermutations = new int[threadCount];

            using (var reader = new StreamReader(studentsFile))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null }))
            {
                var list = new List<Student>();
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    int.TryParse(csv["Grade"], out var parsedGrade);
                    list.Add(new Student(
                        csv["Name"].Trim(),
                        parsedGrade,
                        csv["Class"].Trim(),
                        csv["Ma"].Trim(),
                        csv["En"].Trim(),
                        csv["De"].Trim(),
                        csv["Ph"].Trim(),
                        csv["WP"].Trim(),
                        csv["Sp"].Trim(),
                        csv["Extrawurst"].Trim()
                    ));
                }
                students = list.ToArray();
            }

            if (grade != -1)
                students = students.Where(s => s.Grade == grade).ToArray();

            for (int i = 0; i < threadCount; i++)
            {
                int threadNumber = i;

                ThreadPool.QueueUserWorkItem(o =>
                {
                    Random random;
                    lock (MasterRandom)
                        random = new Random(MasterRandom.Next());

                    while (!abortRequested)
                    {
                        var studentsRandom = students.Select(s => s with { Week = random.Next(2) switch { 0 => "A", 1 => "B" } }).ToArray();
                        (var average, var max) = CalculateDistance(studentsRandom);
                        testedPermutations[threadNumber] += 1;
                        if (max > maxAbsDistance) continue;
                        if (average > maxAvgDistance) continue;
                        lock (lockObject)
                        {
                            successCount += 1;
                            Save(Path.GetFullPath("."), successCount, average, max, studentsRandom);
                        }
                    }
                });
            }

            Console.WriteLine("press any key to abort");
            while (!Console.KeyAvailable && successCount < 10)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Console.CursorLeft = 0;
                Console.Write($"successes: {successCount}, tries: {testedPermutations.Sum():N0}");
            }

            Console.WriteLine($"{Environment.NewLine}aborting...");
            abortRequested = true;
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        private static void Save(string folder, int resultNumber, decimal avg, int max, Student[] students)
        {
            var name = $"{resultNumber:D4}_{avg:N3}average_{max}max";
            var text = JsonSerializer.Serialize(students);
            File.WriteAllText(Path.Combine(folder, $"{name}.json"), text);

            using var file = File.Create($"{name}.txt");
            using var writer = new StreamWriter(file);
            PrintCourses(writer, students);
            PrintStudents(writer, students);
            PrintExtraWurst(writer, students);
        }

        private static (decimal Average, int Max) CalculateDistance(IEnumerable<Student> students)
        {
            IEnumerable<int> distances(Func<Student, string> getCourse)
            {
                return students
                    .GroupBy(s => getCourse(s))
                    .Where(g => g.Key != default)
                    .Select(g => g.Count() <= 6
                        ? g.Count(s => s.Week == "A") == 0 || g.Count(s => s.Week == "B") == 0 ? 0 : 1_000_000
                        : Math.Abs(g.Count(s => s.Week == "A") - g.Count(s => s.Week == "B")));
            }

            // student groups that should not be divided are checked here
            Dictionary<string, string> extraWürste = new();
            foreach (var student in students)
            {
                if (string.IsNullOrWhiteSpace(student.ExtraWurst))
                    continue;
                if (!extraWürste.TryGetValue(student.ExtraWurst, out var week))
                    extraWürste.Add(student.ExtraWurst, student.Week);
                else if (student.Week != week)
                    return (1_000_000, 1_000_000);
            }

            var (sum, max, count) = Subjects
                .Select(s => s.getCourse)
                .SelectMany(distances)
                .Aggregate((Sum: 0, Max: 0, Count: 0), (acc, distance) => (acc.Sum + distance, Math.Max(acc.Max, distance), acc.Count + 1));
            return (sum / (decimal)count, max);
        }

        private static readonly IEnumerable<(string subject, Func<Student, string> getCourse)> Subjects =
            new (string, Func<Student, string>)[]
            {
                ("Klasse", s => s.Class),
                ("De", s => s.De),
                ("En", s => s.En),
                ("Ma", s => s.Ma),
                ("Ph", s => s.Ph),
                ("WP", s => s.WP),
                ("Sport", s => s.Sp)
            };

        private static void PrintCourses(StreamWriter writer, IEnumerable<Student> students)
        {
            foreach ((var subject, var getCourse) in Subjects)
            {
                var groups = students.GroupBy(getCourse).Where(g => g.Key != default);
                writer.WriteLine(subject);
                foreach (var group in groups)
                    writer.WriteLine($"    {group.Key}: {group.Where(s => s.Week == "A").Count()} - {group.Where(s => s.Week == "B").Count()}");
            }
        }

        private static void PrintStudents(StreamWriter writer, IEnumerable<Student> students)
        {
            foreach (var group in students.GroupBy(s => s.Class).OrderBy(g => g.Key))
            {
                writer.WriteLine(group.Key);
                foreach (var student in group.OrderBy(s => s.Week).ThenBy(s => s.Name))
                    writer.WriteLine($"    {student.Week}  {student.Name}");
            }
        }

        private static void PrintExtraWurst(StreamWriter writer, IEnumerable<Student> students)
        {
            var extraWürste = students.Where(s => !string.IsNullOrWhiteSpace(s.ExtraWurst)).ToArray();
            if (extraWürste.Length == 0)
                return;
            var groups = extraWürste.GroupBy(s => s.ExtraWurst);
            foreach (var group in groups)
            {
                writer.WriteLine(group.Key);
                foreach (var student in group)
                    writer.WriteLine($"    {student.Week}  {student.Name}");
            }
        }
    }
}
