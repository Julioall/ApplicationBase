using Application.Domain.Model.Students;
using Raven.Client.Documents.Indexes;
using System.Linq;

namespace Application.Infrastructure.Indexes
{
    public class Student_ByIdNumber : AbstractIndexCreationTask<Student>
    {
        public Student_ByIdNumber()
        {
            Map = students => from student in students
                              select new
                              {
                                  student.IdNumber
                              };
        }
    }
}
