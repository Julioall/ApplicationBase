using System.Reflection;
using Application.Service.Education;
using Xunit;

namespace Application.Tests.Services
{
    public class EducationReportImportProcessorHashTests
    {
        [Fact]
        public void GetDeterministicPositiveHash_Should_Be_Stable_For_Same_Input()
        {
            var first = InvokeHash("Unidade Curricular XYZ");
            var second = InvokeHash("Unidade Curricular XYZ");

            Assert.True(first > 0);
            Assert.Equal(first, second);
        }

        [Fact]
        public void GetDeterministicPositiveHash_Should_Trim_Input()
        {
            var trimmed = InvokeHash("UC-123");
            var withWhitespace = InvokeHash("  UC-123   ");

            Assert.Equal(trimmed, withWhitespace);
        }

        [Fact]
        public void GetDeterministicPositiveHash_Should_Return_Zero_For_Empty()
        {
            Assert.Equal(0, InvokeHash(string.Empty));
            Assert.Equal(0, InvokeHash("   "));
        }

        private static int InvokeHash(string? text)
        {
            var method = typeof(EducationReportImportProcessor)
                .GetMethod("GetDeterministicPositiveHash", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            return (int)(method!.Invoke(null, new object?[] { text }) ?? 0);
        }
    }
}
