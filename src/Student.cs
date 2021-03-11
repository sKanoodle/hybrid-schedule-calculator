using System;
using System.Collections.Generic;
using System.Text;

namespace HybridScheduleCalculator
{
    internal class Student
    {
        public string Name { get; set; }
        public int Grade { get; set; }
        public string Class { get; set; }
        public string Ma { get; set; }
        public string En { get; set; }
        public string De { get; set; }
        public string Ph { get; set; }
        public string WP { get; set; }
        public string Sp { get; set; }
        public string Week { get; set; }

        public Student() { }

        public Student(string name, int grade, string @class, string ma, string en, string de, string ph, string wp, string sp)
        {
            Name = name;
            Grade = grade;
            Class = @class;
            Ma = ma;
            En = en;
            De = de;
            Ph = ph;
            WP = wp;
            Sp = sp;
        }

        private Student(string name, int grade, string @class, string ma, string en, string de, string ph, string wp, string sp, string week)
            : this(name, grade, @class, ma, en, de, ph, wp, sp)
        {
            Week = week;
        }

        public Student WithWeek(string week) => new Student(Name, Grade, Class, Ma, En, De, Ph, WP, Sp, week);
    }
}
