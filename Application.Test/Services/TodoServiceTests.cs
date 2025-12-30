using Application.Domain.Exceptions;
using Application.Domain.Model.Todo;
using Application.Domain.Model.Todo.Dtos;
using Application.Service.Interface;
using Application.Tests.Setup;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Services
{
    public class TodoServiceTests : BaseTest
    {
        private readonly ITodoService _todoService;

        public TodoServiceTests()
        {
            _todoService = _serviceProvider.GetRequiredService<ITodoService>();
        }

        [Fact]
        public async Task CreateTask_Should_Set_Created_Metadata_And_Defaults()
        {
            var dto = new CreateTodoTaskDto
            {
                Title = "Prepare onboarding",
                Priority = 2,
                Steps = new List<CreateTodoStepDto>
                {
                    new() { Title = "Collect documents" },
                    new() { Title = "Schedule kickoff" }
                }
            };

            var created = await _todoService.CreateAsync("users/1-A", dto);
            await _asyncSession.SaveChangesAsync();

            var loaded = await _todoService.GetByIdAsync(created.Id);

            Assert.Equal("Prepare onboarding", loaded.Title);
            Assert.Equal(TodoStatus.NotStarted, loaded.Status);
            Assert.Equal("users/1-A", loaded.CreatedByUserId);
            Assert.Equal(2, loaded.Steps.Count);
            Assert.True(loaded.CreatedAt != default);
        }

        [Fact]
        public async Task Update_Status_To_Done_Should_Complete_Steps_And_Set_CompletedAt()
        {
            var dto = new CreateTodoTaskDto
            {
                Title = "Finalize report",
                Steps = new List<CreateTodoStepDto>
                {
                    new() { Title = "Draft" },
                    new() { Title = "Review" }
                }
            };

            var created = await _todoService.CreateAsync("users/2-A", dto);
            await _asyncSession.SaveChangesAsync();

            var updated = await _todoService.UpdateAsync(created.Id, new UpdateTodoTaskDto
            {
                Status = TodoStatus.Done
            });
            await _asyncSession.SaveChangesAsync();

            var loaded = await _todoService.GetByIdAsync(updated.Id);

            Assert.Equal(TodoStatus.Done, loaded.Status);
            Assert.NotNull(loaded.CompletedAt);
            Assert.All(loaded.Steps, step => Assert.True(step.IsCompleted));
        }

        [Fact]
        public async Task ReorderSteps_Should_Update_Order()
        {
            var dto = new CreateTodoTaskDto
            {
                Title = "Publish release",
                Steps = new List<CreateTodoStepDto>
                {
                    new() { Title = "Tag version" },
                    new() { Title = "Deploy" },
                    new() { Title = "Announce" }
                }
            };

            var created = await _todoService.CreateAsync("users/3-A", dto);
            await _asyncSession.SaveChangesAsync();

            var reversedIds = created.Steps.Select(s => s.Id).Reverse().ToArray();
            var reordered = await _todoService.ReorderStepsAsync(created.Id, new ReorderTodoStepsDto
            {
                StepIds = reversedIds
            });
            await _asyncSession.SaveChangesAsync();

            var orders = reordered.Steps.Select(s => s.Order).ToArray();
            Assert.Equal(new[] { 0, 1, 2 }, orders);
            Assert.Equal(reversedIds[0], reordered.Steps.First().Id);
        }

        [Fact]
        public async Task CreateTask_Should_Validate_Context_Consistency()
        {
            var dto = new CreateTodoTaskDto
            {
                Title = "Context check",
                ContextId = "schools/1-A"
            };

            await Assert.ThrowsAsync<ValidationException>(() => _todoService.CreateAsync("users/4-A", dto));
        }
    }
}
