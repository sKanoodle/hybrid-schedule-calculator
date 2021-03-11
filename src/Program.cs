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
        private const string BaseFolder = @"";
        private const int MaxAverageDistance = 2;
        private const int MaxAbsoluteDistance = 1;

        static void Main(string[] args)
        {
            //ReadAndPrint(@"");

            Calculate();

            Console.Read();
        }

        private static void Calculate()
        {
            IEnumerable<Student> students;
            int testedPermutations = 0;

            using (var reader = new StreamReader(@""))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null }))
                students = csv.GetRecords<Student>().ToArray();

            students = students/*.Where(s => s.Grade == 9)*/.ToArray();

            List<Student[]> perfects = new List<Student[]>();

            for (int i = 0; i < 8; i++)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    Random random;
                    lock (MasterRandom)
                        random = new Random(MasterRandom.Next());

                    while (perfects.Count < 1)
                    {
                        var studentsRandom = students.Select(s => s with { Week = random.Next(2) switch { 0 => "A", 1 => "B" } }).ToArray();
                        (var average, var max) = CalculateDistance(studentsRandom);
                        Interlocked.Increment(ref testedPermutations);
                        if (max > MaxAbsoluteDistance) continue;
                        if (average > MaxAverageDistance) continue;
                        lock (perfects)
                            perfects.Add(studentsRandom);
                    }
                });
            }

            while (perfects.Count < 1)
                Thread.Sleep(TimeSpan.FromSeconds(1));

            Thread.Sleep(TimeSpan.FromSeconds(0.5));

            Console.WriteLine($"{testedPermutations:N0}");
            Save(perfects);
        }

        private static void ReadAndPrint(string path)
        {
            var students = JsonSerializer.Deserialize<Student[]>(File.ReadAllText(path));
            PrintCourses(students);
            PrintStudents(students);
        }

        private static void Save(IEnumerable<Student[]> lists)
        {
            int i = 1;
            foreach (var list in lists)
            {
                var text = JsonSerializer.Serialize(list);
                File.WriteAllText(Path.Combine(BaseFolder, $"success{i}.json"), text);
            }

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

            var foo = Subjects
                .Select(s => s.getCourse)
                .SelectMany(distances)
                .Aggregate((Sum: 0, Max: 0, Count: 0), (acc, distance) => (acc.Sum + distance, Math.Max(acc.Max, distance), acc.Count + 1));
            return (foo.Sum / (decimal)foo.Count, foo.Max);
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

        private static void PrintCourses(IEnumerable<Student> students)
        {
            foreach ((var subject, var getCourse) in Subjects)
            {
                var groups = students.GroupBy(getCourse).Where(g => g.Key != default);
                Console.WriteLine(subject);
                foreach (var group in groups)
                    Console.WriteLine($"    {group.Key}: {group.Where(s => s.Week == "A").Count()} - {group.Where(s => s.Week == "B").Count()}");
            }

        }

        private static void PrintStudents(IEnumerable<Student> students)
        {
            foreach (var group in students.GroupBy(s => s.Class).OrderBy(g => g.Key))
            {
                Console.WriteLine(group.Key);
                foreach (var student in group.OrderBy(s => s.Week).ThenBy(s => s.Name))
                    Console.WriteLine($"    {student.Week}  {student.Name}");
            }
        }
    }
}
