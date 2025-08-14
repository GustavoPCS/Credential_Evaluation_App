using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CredentialEvaluationApp.Models
{
    public class Student
    {
        public int Student_Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DOB { get; set; }
        public string Term { get; set; }
        public double? HSGPA { get; set; }
        public double? UniGPA { get; set; }
        public List<Transcript> Transcripts { get; set; }
    }
}

