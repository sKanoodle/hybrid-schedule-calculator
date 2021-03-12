using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HybridScheduleCalculator
{
    interface IRandomizeWeeksStrategy
    {
        Student[] GetRandomizedStudents(Random random);
    }

    abstract class RandomizeWeeksBase
    {
        protected static string GetRandomWeek(Random random) => random.Next(2) switch { 0 => "A", 1 => "B" };

        protected static IEnumerable<Student> SimpleRandomize(IEnumerable<Student>? students, Random random)
        {
            if (students == null)
                return Enumerable.Empty<Student>();

            return students
                .Select(s => s with { Week = GetRandomWeek(random) });
        }
    }

    class RandomizeWeeksDefaultStrategy : RandomizeWeeksBase, IRandomizeWeeksStrategy
    {
        private readonly Student[] Students;

        public RandomizeWeeksDefaultStrategy(Student[] students)
        {
            Students = students;
        }

        public Student[] GetRandomizedStudents(Random random)
        {
            return SimpleRandomize(Students, random).ToArray();
        }
    }

    class RandomizeWeeksExtraWurstStrategy : RandomizeWeeksBase, IRandomizeWeeksStrategy
    {
        private static readonly Dictionary<string, List<Student>> ExtraWurstAssignments = new();

        public RandomizeWeeksExtraWurstStrategy(Student[] students)
        {
            foreach (var student in students)
            {
                var extraWurstValue = String.IsNullOrWhiteSpace(student.ExtraWurst) ? String.Empty : student.ExtraWurst;
                if (!ExtraWurstAssignments.TryGetValue(extraWurstValue, out var list))
                    ExtraWurstAssignments.Add(extraWurstValue, list = new List<Student>());
                list.Add(student);
            }
        }

        public Student[] GetRandomizedStudents(Random random)
        {
            return ExtraWurstAssignments
                .Where(kvp => kvp.Key != String.Empty) // String.Empty is marker for no group set
                .Select(kvp => (List: kvp.Value, Week: GetRandomWeek(random)))
                .SelectMany(t => t.List.Select(s => s with { Week = t.Week }))
                .Concat(SimpleRandomize(ExtraWurstAssignments.GetValueOrDefault(String.Empty), random))
                .ToArray();
        }
    }
}
