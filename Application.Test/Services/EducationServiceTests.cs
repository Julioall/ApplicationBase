using System.Text;
using System.Text.Json;
using Application.Domain.Model.Dtos;
using Application.Domain.Model.Education;
using Application.Service.Interface;
using Application.Tests.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Services
{
    public class EducationServiceTests : BaseTest
    {
        private readonly IEducationService _educationService;

        public EducationServiceTests()
        {
            _educationService = _serviceProvider.GetService<IEducationService>()
                ?? throw new Exception($"{nameof(IEducationService)} nÃ£o foi encontrado");
        }

        [Fact]
        public async Task ImportCourses_Should_Create_Hierarchy_And_Period()
        {
            var json = BuildCoursesJson();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var result = await _educationService.ImportCoursesAsync(stream, "cursos.json");
            await _asyncSession.SaveChangesAsync();

            Assert.Equal(2, result.Processed);
            Assert.Equal(2, result.CreatedUcs);
            Assert.Equal(0, result.UpdatedUcs);
            Assert.Equal(2, result.Linked);
            Assert.Empty(result.Errors);

            var schools = await _educationService.GetSchoolsAsync();
            Assert.Single(schools);
            var school = schools.First();
            Assert.Equal("Fatec SENAI Roberto Mange", school.Name);

            var programs = await _educationService.GetProgramsBySchoolAsync(school.Id!);
            Assert.Single(programs);
            var program = programs.First();
            Assert.Equal("Tecnico em Informatica para Internet", program.Name);

            var classes = await _educationService.GetClassesByProgramAsync(program.Id!);
            Assert.Single(classes);
            var turma = classes.First();
            Assert.Equal("1005459 - Tecnico em Informatica para Internet - 00002/2025", turma.Name);
            Assert.Equal(1704067200, turma.StartDate);
            Assert.Equal(1709251200, turma.EndDate);

            var ucs = await _educationService.GetUcsByClassAsync(turma.Id!);
            Assert.Equal(2, ucs.Count);
            Assert.Contains(ucs, uc => uc.Fullname.Contains("Logica"));
            Assert.Contains(ucs, uc => uc.Fullname.Contains("Fundamentos"));
        }

        [Fact]
        public async Task GetUcsByClass_Should_Filter_By_Search_Term()
        {
            var json = BuildCoursesJson();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await _educationService.ImportCoursesAsync(stream, "cursos.json");
            await _asyncSession.SaveChangesAsync();

            var schools = await _educationService.GetSchoolsAsync();
            var program = (await _educationService.GetProgramsBySchoolAsync(schools.First().Id!)).First();
            var classDoc = (await _educationService.GetClassesByProgramAsync(program.Id!)).First();

            var filtered = await _educationService.GetUcsByClassAsync(classDoc.Id!, "Logica");
            Assert.Single(filtered);
            Assert.Contains(filtered, uc => uc.Fullname.Contains("Logica"));

            var none = await _educationService.GetUcsByClassAsync(classDoc.Id!, "Inexistente");
            Assert.Empty(none);
        }

        [Fact]
        public async Task ImportCourses_Should_Be_Idempotent()
        {
            var json = BuildCoursesJson();
            using var first = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await _educationService.ImportCoursesAsync(first, "cursos.json");
            await _asyncSession.SaveChangesAsync();

            using var second = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var result = await _educationService.ImportCoursesAsync(second, "cursos.json");
            await _asyncSession.SaveChangesAsync();

            Assert.Equal(2, result.Processed);
            Assert.Empty(result.Errors);
            Assert.Equal(0, result.CreatedUcs);
            Assert.Equal(2, result.UpdatedUcs);
        }

        [Fact]
        public async Task SearchUcs_Should_Filter_By_Class()
        {
            var json = BuildCoursesJson();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await _educationService.ImportCoursesAsync(stream, "cursos.json");
            await _asyncSession.SaveChangesAsync();

            var schools = await _educationService.GetSchoolsAsync();
            var program = (await _educationService.GetProgramsBySchoolAsync(schools.First().Id!)).First();
            var classDoc = (await _educationService.GetClassesByProgramAsync(program.Id!)).First();

            var paged = await _educationService.SearchUcsAsync(new PaginationQuery
            {
                PageNumber = 1,
                PageSize = 10,
                Search = "Logica"
            }, classDoc.Id!);

            Assert.Equal(1, paged.Total);
            Assert.Single(paged.Items);
            Assert.Contains("Logica", paged.Items.First().Fullname);
        }

        private static string BuildCoursesJson()
        {
            var courses = new[]
            {
                new
                {
                    id = 27537,
                    fullname = "Fundamentos de Web Design",
                    startdate = 1704067200,
                    enddate = 1706659200,
                    viewurl = "https://ead.test/course/view.php?id=27537",
                    courseimage = "https://ead.test/course/image1.jpg",
                    coursecategory = "1005459 - Tecnico em Informatica para Internet - 00002/2025",
                    summary = "Tecnico em Informatica para Internet<br /><br />Fatec SENAI Roberto Mange<br /><br />Periodo: 01/01/2024 a 31/01/2024<br /><br />"
                },
                new
                {
                    id = 27535,
                    fullname = "Logica de Programacao",
                    startdate = 1706745600,
                    enddate = 1709251200,
                    viewurl = "https://ead.test/course/view.php?id=27535",
                    courseimage = "https://ead.test/course/image2.jpg",
                    coursecategory = "1005459 - Tecnico em Informatica para Internet - 00002/2025",
                    summary = "Tecnico em Informatica para Internet<br /><br />Fatec SENAI Roberto Mange<br /><br />Periodo: 01/02/2024 a 01/03/2024<br /><br />"
                }
            };

            var payload = new[]
            {
                new
                {
                    error = false,
                    data = new
                    {
                        courses
                    }
                }
            };

            return JsonSerializer.Serialize(payload);
        }
    }
}
