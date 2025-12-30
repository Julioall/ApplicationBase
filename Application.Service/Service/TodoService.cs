using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface.Todo;
using Application.Domain.Model.Todo;
using Application.Domain.Model.Todo.Dtos;
using Application.Service.Interface;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Service.Service
{
    public class TodoService : ITodoService
    {
        private readonly ITodoRepository _repository;
        private readonly IValidator<TodoTask> _taskValidator;
        private readonly IValidator<CreateTodoTaskDto> _createValidator;
        private readonly IValidator<UpdateTodoTaskDto> _updateValidator;
        private readonly IValidator<CreateTodoStepDto> _createStepValidator;
        private readonly IValidator<UpdateTodoStepDto> _updateStepValidator;
        private readonly IValidator<ReorderTodoStepsDto> _reorderValidator;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public TodoService(
            ITodoRepository repository,
            IValidator<TodoTask> taskValidator,
            IValidator<CreateTodoTaskDto> createValidator,
            IValidator<UpdateTodoTaskDto> updateValidator,
            IValidator<CreateTodoStepDto> createStepValidator,
            IValidator<UpdateTodoStepDto> updateStepValidator,
            IValidator<ReorderTodoStepsDto> reorderValidator,
            IStringLocalizer<SharedResource> localizer)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _taskValidator = taskValidator ?? throw new ArgumentNullException(nameof(taskValidator));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
            _createStepValidator = createStepValidator ?? throw new ArgumentNullException(nameof(createStepValidator));
            _updateStepValidator = updateStepValidator ?? throw new ArgumentNullException(nameof(updateStepValidator));
            _reorderValidator = reorderValidator ?? throw new ArgumentNullException(nameof(reorderValidator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<IReadOnlyList<TodoTaskDto>> GetTasksAsync(TodoTaskSearchQuery query)
        {
            var normalized = NormalizeQuery(query ?? new TodoTaskSearchQuery());
            var tasks = await _repository.SearchAsync(normalized);
            return tasks.Select(ToDto).ToList();
        }

        public async Task<TodoTaskDto> GetByIdAsync(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            var task = await _repository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException(_localizer["TodoTaskNotFound", id]);
            }

            return ToDto(task);
        }

        public async Task<TodoTaskDto> CreateAsync(string currentUserId, CreateTodoTaskDto dto)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUserId);
            ArgumentNullException.ThrowIfNull(dto);

            await _createValidator.ValidateAndThrowAsync(dto);

            var task = new TodoTask
            {
                Title = dto.Title.Trim(),
                Description = Normalize(dto.Description),
                Status = dto.Status ?? TodoStatus.NotStarted,
                Category = Normalize(dto.Category),
                Priority = dto.Priority,
                StartDate = dto.StartDate,
                DueDate = dto.DueDate,
                ContextType = Normalize(dto.ContextType),
                ContextId = Normalize(dto.ContextId),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = Normalize(currentUserId),
                AssignedToUserId = Normalize(dto.AssignedToUserId),
                IsArchived = false,
                Steps = new List<TodoStep>()
            };

            if (dto.Steps != null && dto.Steps.Any())
            {
                var order = 0;
                foreach (var stepDto in dto.Steps)
                {
                    await _createStepValidator.ValidateAndThrowAsync(stepDto);
                    task.Steps.Add(new TodoStep
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = stepDto.Title.Trim(),
                        IsCompleted = false,
                        Order = order++
                    });
                }
            }

            if (task.Status == TodoStatus.Done)
            {
                CompleteAllSteps(task);
                task.CompletedAt = DateTime.UtcNow;
            }

            await _taskValidator.ValidateAndThrowAsync(task);
            await _repository.AddAsync(task);

            return ToDto(task);
        }

        public async Task<TodoTaskDto> UpdateAsync(string id, UpdateTodoTaskDto dto)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(dto);

            await _updateValidator.ValidateAndThrowAsync(dto);

            var task = await _repository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException(_localizer["TodoTaskNotFound", id]);
            }

            task.Steps ??= new List<TodoStep>();
            if (task.CreatedAt == default)
            {
                task.CreatedAt = DateTime.UtcNow;
            }

            if (dto.Title != null)
            {
                task.Title = dto.Title.Trim();
            }

            task.Description = dto.Description != null ? Normalize(dto.Description) : task.Description;
            task.Category = dto.Category != null ? Normalize(dto.Category) : task.Category;
            task.Priority = dto.Priority ?? task.Priority;
            task.StartDate = dto.StartDate ?? task.StartDate;
            task.DueDate = dto.DueDate ?? task.DueDate;
            task.ContextType = dto.ContextType != null ? Normalize(dto.ContextType) : task.ContextType;
            task.ContextId = dto.ContextId != null ? Normalize(dto.ContextId) : task.ContextId;
            task.AssignedToUserId = dto.AssignedToUserId != null ? Normalize(dto.AssignedToUserId) : task.AssignedToUserId;

            if (dto.Status.HasValue)
            {
                task.Status = dto.Status.Value;
                if (task.Status == TodoStatus.Done)
                {
                    CompleteAllSteps(task);
                    task.CompletedAt ??= DateTime.UtcNow;
                }
                else
                {
                    task.CompletedAt = null;
                }
            }

            await _taskValidator.ValidateAndThrowAsync(task);
            await _repository.UpdateAsync(task);

            return ToDto(task);
        }

        public async Task ArchiveAsync(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);

            var task = await _repository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException(_localizer["TodoTaskNotFound", id]);
            }

            task.IsArchived = true;
            await _repository.UpdateAsync(task);
        }

        public async Task<TodoTaskDto> AddStepAsync(string taskId, CreateTodoStepDto dto)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            ArgumentNullException.ThrowIfNull(dto);

            await _createStepValidator.ValidateAndThrowAsync(dto);

            var task = await _repository.GetByIdAsync(taskId);
            if (task == null)
            {
                throw new NotFoundException(_localizer["TodoTaskNotFound", taskId]);
            }

            task.Steps ??= new List<TodoStep>();
            var order = task.Steps.Any() ? task.Steps.Max(s => s.Order) + 1 : 0;
            task.Steps.Add(new TodoStep
            {
                Id = Guid.NewGuid().ToString(),
                Title = dto.Title.Trim(),
                IsCompleted = false,
                Order = order
            });

            if (task.Status == TodoStatus.Done)
            {
                task.Status = TodoStatus.InProgress;
                task.CompletedAt = null;
            }
            else if (task.Status == TodoStatus.NotStarted)
            {
                task.Status = TodoStatus.InProgress;
            }

            await _taskValidator.ValidateAndThrowAsync(task);
            await _repository.UpdateAsync(task);

            return ToDto(task);
        }

        public async Task<TodoTaskDto> UpdateStepAsync(string taskId, string stepId, UpdateTodoStepDto dto)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            ArgumentException.ThrowIfNullOrWhiteSpace(stepId);
            ArgumentNullException.ThrowIfNull(dto);

            await _updateStepValidator.ValidateAndThrowAsync(dto);

            var task = await _repository.GetByIdAsync(taskId);
            if (task == null)
            {
                throw new NotFoundException(_localizer["TodoTaskNotFound", taskId]);
            }

            task.Steps ??= new List<TodoStep>();
            var step = FindStep(task, stepId);
            if (step == null)
            {
                throw new NotFoundException(_localizer["TodoStepNotFound", stepId]);
            }

            var wasCompleted = step.IsCompleted;

            if (dto.Title != null)
            {
                step.Title = dto.Title.Trim();
            }

            if (dto.IsCompleted.HasValue)
            {
                step.IsCompleted = dto.IsCompleted.Value;
                if (step.IsCompleted && !wasCompleted)
                {
                    step.CompletedAt = DateTime.UtcNow;
                }
                else if (!step.IsCompleted)
                {
                    step.CompletedAt = null;
                }
            }

            SyncStatusWithSteps(task);

            await _taskValidator.ValidateAndThrowAsync(task);
            await _repository.UpdateAsync(task);

            return ToDto(task);
        }

        public async Task<TodoTaskDto> ReorderStepsAsync(string taskId, ReorderTodoStepsDto dto)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            ArgumentNullException.ThrowIfNull(dto);

            await _reorderValidator.ValidateAndThrowAsync(dto);

            var task = await _repository.GetByIdAsync(taskId);
            if (task == null)
            {
                throw new NotFoundException(_localizer["TodoTaskNotFound", taskId]);
            }

            task.Steps ??= new List<TodoStep>();
            if (dto.StepIds.Count != task.Steps.Count)
            {
                throw new BusinessException(_localizer["TodoStepOrderInvalid"]);
            }

            var stepsById = task.Steps.ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
            var newOrder = new List<TodoStep>();

            foreach (var id in dto.StepIds)
            {
                if (!stepsById.TryGetValue(id, out var step))
                {
                    throw new NotFoundException(_localizer["TodoStepNotFound", id]);
                }
                newOrder.Add(step);
            }

            for (var index = 0; index < newOrder.Count; index++)
            {
                newOrder[index].Order = index;
            }

            task.Steps = newOrder;

            SyncStatusWithSteps(task);

            await _taskValidator.ValidateAndThrowAsync(task);
            await _repository.UpdateAsync(task);

            return ToDto(task);
        }

        public async Task<TodoTaskDto> DeleteStepAsync(string taskId, string stepId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
            ArgumentException.ThrowIfNullOrWhiteSpace(stepId);

            var task = await _repository.GetByIdAsync(taskId);
            if (task == null)
            {
                throw new NotFoundException(_localizer["TodoTaskNotFound", taskId]);
            }

            task.Steps ??= new List<TodoStep>();
            var step = FindStep(task, stepId);
            if (step == null)
            {
                throw new NotFoundException(_localizer["TodoStepNotFound", stepId]);
            }

            task.Steps.Remove(step);
            ReassignOrders(task.Steps);

            SyncStatusWithSteps(task);

            await _taskValidator.ValidateAndThrowAsync(task);
            await _repository.UpdateAsync(task);

            return ToDto(task);
        }

        private TodoTaskSearchQuery NormalizeQuery(TodoTaskSearchQuery query)
        {
            return new TodoTaskSearchQuery
            {
                ContextType = Normalize(query.ContextType),
                ContextId = Normalize(query.ContextId),
                AssignedToUserId = Normalize(query.AssignedToUserId),
                IncludeArchived = query.IncludeArchived
            };
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static TodoStep? FindStep(TodoTask task, string stepId)
        {
            return task.Steps.FirstOrDefault(s => string.Equals(s.Id, stepId, StringComparison.OrdinalIgnoreCase));
        }

        private static void ReassignOrders(ICollection<TodoStep> steps)
        {
            var ordered = steps.OrderBy(s => s.Order).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].Order = i;
            }
        }

        private void SyncStatusWithSteps(TodoTask task)
        {
            if (task.Steps == null || !task.Steps.Any())
            {
                if (task.Status == TodoStatus.Done && !task.CompletedAt.HasValue)
                {
                    task.CompletedAt = DateTime.UtcNow;
                }
                return;
            }

            var allCompleted = task.Steps.All(s => s.IsCompleted);
            if (allCompleted)
            {
                if (task.Status != TodoStatus.Done)
                {
                    task.Status = TodoStatus.Done;
                }

                task.CompletedAt ??= DateTime.UtcNow;
                return;
            }

            if (task.Status == TodoStatus.Done || task.CompletedAt.HasValue)
            {
                task.Status = TodoStatus.InProgress;
                task.CompletedAt = null;
            }
            else if (task.Status == TodoStatus.NotStarted)
            {
                task.Status = TodoStatus.InProgress;
            }
        }

        private static void CompleteAllSteps(TodoTask task)
        {
            if (task.Steps == null)
            {
                task.Steps = new List<TodoStep>();
            }

            foreach (var step in task.Steps)
            {
                step.IsCompleted = true;
                step.CompletedAt ??= DateTime.UtcNow;
            }
        }

        private static TodoTaskDto ToDto(TodoTask task)
        {
            return new TodoTaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Category = task.Category,
                Priority = task.Priority,
                StartDate = task.StartDate,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                ContextType = task.ContextType,
                ContextId = task.ContextId,
                CreatedByUserId = task.CreatedByUserId,
                CreatedAt = task.CreatedAt,
                AssignedToUserId = task.AssignedToUserId,
                IsArchived = task.IsArchived,
                Steps = (task.Steps ?? new List<TodoStep>())
                    .OrderBy(s => s.Order)
                    .Select(s => new TodoStepDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        IsCompleted = s.IsCompleted,
                        Order = s.Order,
                        CompletedAt = s.CompletedAt
                    })
                    .ToList()
            };
        }
    }
}
