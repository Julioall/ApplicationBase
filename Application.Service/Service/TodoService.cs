using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Interface.Todo;
using Application.Domain.Model.Todo;
using Application.Domain.Model.Todo.Dtos;
using Application.Service.Interface;
using HtmlAgilityPack;
using FluentValidation;
using Ganss.Xss;
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
        private readonly ITodoImageRepository _todoImageRepository;
        private readonly HtmlSanitizer _htmlSanitizer;

        public TodoService(
            ITodoRepository repository,
            IValidator<TodoTask> taskValidator,
            IValidator<CreateTodoTaskDto> createValidator,
            IValidator<UpdateTodoTaskDto> updateValidator,
            IValidator<CreateTodoStepDto> createStepValidator,
            IValidator<UpdateTodoStepDto> updateStepValidator,
            IValidator<ReorderTodoStepsDto> reorderValidator,
            IStringLocalizer<SharedResource> localizer,
            ITodoImageRepository todoImageRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _taskValidator = taskValidator ?? throw new ArgumentNullException(nameof(taskValidator));
            _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
            _createStepValidator = createStepValidator ?? throw new ArgumentNullException(nameof(createStepValidator));
            _updateStepValidator = updateStepValidator ?? throw new ArgumentNullException(nameof(updateStepValidator));
            _reorderValidator = reorderValidator ?? throw new ArgumentNullException(nameof(reorderValidator));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _todoImageRepository = todoImageRepository ?? throw new ArgumentNullException(nameof(todoImageRepository));
            _htmlSanitizer = BuildSanitizer();
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

            var sanitizedDescription = await ProcessDescriptionAsync(dto.Description, currentUserId);
            var categories = NormalizeCategories(dto.Categories, dto.Category);
            var recurrence = NormalizeRecurrence(dto.Recurrence);
            var assignees = NormalizeAssignees(dto.Assignees);

            var task = new TodoTask
            {
                Title = dto.Title.Trim(),
                Description = sanitizedDescription,
                Status = dto.Status ?? TodoStatus.NotStarted,
                Category = categories.FirstOrDefault(),
                Categories = categories.ToList(),
                Priority = dto.Priority,
                StartDate = dto.StartDate,
                DueDate = dto.DueDate,
                ContextType = Normalize(dto.ContextType),
                ContextId = Normalize(dto.ContextId),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = Normalize(currentUserId),
                AssignedToUserId = Normalize(dto.AssignedToUserId),
                Assignees = assignees.ToList(),
                IsArchived = false,
                IsAllDay = dto.IsAllDay,
                Recurrence = recurrence,
                RecurrenceGroupId = recurrence != null && recurrence.Type != TodoRecurrenceType.None
                    ? Guid.NewGuid().ToString()
                    : null,
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

            var sanitizedDescription = dto.Description != null ? await ProcessDescriptionAsync(dto.Description, task.CreatedByUserId) : null;
            IReadOnlyCollection<string>? categories = null;
            if (dto.Categories != null || dto.Category != null)
            {
                categories = NormalizeCategories(dto.Categories, dto.Category);
            }

            var recurrence = dto.Recurrence != null ? NormalizeRecurrence(dto.Recurrence) : null;

            var tasksToUpdate = new List<TodoTask> { task };
            if (dto.ApplyToSeries && !string.IsNullOrWhiteSpace(task.RecurrenceGroupId))
            {
                var series = await _repository.SearchByRecurrenceGroupAsync(task.RecurrenceGroupId);
                tasksToUpdate = series.ToList();
                if (tasksToUpdate.All(t => t.Id != task.Id))
                {
                    tasksToUpdate.Add(task);
                }
            }

            foreach (var target in tasksToUpdate)
            {
                ApplyTaskUpdates(target, dto, sanitizedDescription, categories, recurrence);
                await _taskValidator.ValidateAndThrowAsync(target);
                await _repository.UpdateAsync(target);
            }

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

        public async Task DeleteAsync(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            var task = await _repository.GetByIdAsync(id);
            if (task == null)
            {
                throw new NotFoundException(_localizer["TodoTaskNotFound", id]);
            }

            await _repository.DeleteAsync(id);
        }

        public async Task<string> UploadImageAsync(string uploadedByUserId, Stream stream, string contentType, string fileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(uploadedByUserId);
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

            return await _todoImageRepository.StoreAsync(stream, contentType, fileName, uploadedByUserId);
        }

        public Task<(byte[] Data, string ContentType, string FileName)?> GetImageAsync(string imageId)
        {
            return _todoImageRepository.GetAsync(imageId);
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

        private IEnumerable<TodoAssignee> NormalizeAssignees(IEnumerable<TodoAssigneeDto>? assignees)
        {
            if (assignees == null)
            {
                return Array.Empty<TodoAssignee>();
            }

            return assignees
                .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Name))
                .Select(a => new TodoAssignee
                {
                    Id = Normalize(a.Id),
                    Name = a.Name.Trim(),
                    AvatarUrl = a.AvatarUrl
                });
        }

        private void ApplyTaskUpdates(
            TodoTask task,
            UpdateTodoTaskDto dto,
            string? sanitizedDescription,
            IReadOnlyCollection<string>? categories,
            TodoRecurrence? recurrence)
        {
            task.Steps ??= new List<TodoStep>();
            if (task.CreatedAt == default)
            {
                task.CreatedAt = DateTime.UtcNow;
            }

            if (dto.Title != null)
            {
                task.Title = dto.Title.Trim();
            }

            if (dto.Description != null)
            {
                task.Description = sanitizedDescription;
            }

            if (categories != null)
            {
                task.Categories = categories.ToList();
                task.Category = categories.FirstOrDefault();
            }

            if (dto.Priority.HasValue)
            {
                task.Priority = dto.Priority.Value;
            }

            if (dto.StartDate.HasValue)
            {
                task.StartDate = dto.StartDate.Value;
            }

            if (dto.DueDate.HasValue)
            {
                task.DueDate = dto.DueDate.Value;
            }

            if (dto.ContextType != null)
            {
                task.ContextType = Normalize(dto.ContextType);
            }

            if (dto.ContextId != null)
            {
                task.ContextId = Normalize(dto.ContextId);
            }

            if (dto.AssignedToUserId != null)
            {
                task.AssignedToUserId = Normalize(dto.AssignedToUserId);
            }

            if (dto.Assignees != null)
            {
                task.Assignees = NormalizeAssignees(dto.Assignees).ToList();
            }

            if (dto.IsAllDay.HasValue)
            {
                task.IsAllDay = dto.IsAllDay.Value;
            }

            if (dto.Recurrence != null)
            {
                task.Recurrence = recurrence;
                task.RecurrenceGroupId = recurrence != null && recurrence.Type != TodoRecurrenceType.None
                    ? task.RecurrenceGroupId ?? Guid.NewGuid().ToString()
                    : null;
            }

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
        }

        private async Task<string?> ProcessDescriptionAsync(string? html, string? uploadedByUserId)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imgNodes = doc.DocumentNode.SelectNodes("//img[@src]");
            if (imgNodes != null)
            {
                foreach (var img in imgNodes)
                {
                    var src = img.GetAttributeValue("src", string.Empty);
                    if (string.IsNullOrWhiteSpace(src) || !src.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var replacement = await StoreDataImageAsync(src, uploadedByUserId);
                    if (!string.IsNullOrWhiteSpace(replacement))
                    {
                        img.SetAttributeValue("src", replacement);
                    }
                }
            }

            return SanitizeHtml(doc.DocumentNode.OuterHtml);
        }

        private async Task<string?> StoreDataImageAsync(string dataUri, string? uploadedByUserId)
        {
            var commaIndex = dataUri.IndexOf(',', StringComparison.Ordinal);
            if (commaIndex < 0)
            {
                return null;
            }

            var meta = dataUri[..commaIndex];
            var base64 = dataUri[(commaIndex + 1)..];

            var semicolonIndex = meta.IndexOf(';');
            var contentType = semicolonIndex > 0 ? meta[5..semicolonIndex] : "image/png";

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch
            {
                return null;
            }

            var fileName = $"inline-image{GetExtensionFromContentType(contentType)}";
            await using var stream = new MemoryStream(bytes);
            var imageId = await _todoImageRepository.StoreAsync(stream, contentType, fileName, uploadedByUserId);
            return $"/api/todo/images/{Uri.EscapeDataString(imageId)}";
        }

        private static string GetExtensionFromContentType(string contentType)
        {
            return contentType.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".img"
            };
        }

        private static TodoRecurrence? NormalizeRecurrence(TodoRecurrenceDto? dto)
        {
            if (dto == null || dto.Type == TodoRecurrenceType.None)
            {
                return null;
            }

            return new TodoRecurrence
            {
                Type = dto.Type,
                Interval = dto.Interval <= 0 ? 1 : dto.Interval,
                EndsOn = dto.EndsOn,
                DaysOfWeek = dto.DaysOfWeek?.Distinct().ToList() ?? new List<DayOfWeek>()
            };
        }

        private static TodoRecurrenceDto? ToRecurrenceDto(TodoRecurrence? recurrence)
        {
            if (recurrence == null || recurrence.Type == TodoRecurrenceType.None)
            {
                return null;
            }

            return new TodoRecurrenceDto
            {
                Type = recurrence.Type,
                Interval = recurrence.Interval <= 0 ? 1 : recurrence.Interval,
                EndsOn = recurrence.EndsOn,
                DaysOfWeek = recurrence.DaysOfWeek?.ToList() ?? new List<DayOfWeek>()
            };
        }

        private static IReadOnlyCollection<string> NormalizeCategories(IEnumerable<string>? categories, string? fallbackCategory)
        {
            var list = new List<string>();
            if (categories != null)
            {
                list.AddRange(categories);
            }

            if (!string.IsNullOrWhiteSpace(fallbackCategory))
            {
                list.Add(fallbackCategory);
            }

            var normalized = list
                .Select(c => Normalize(c))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return normalized;
        }

        private string? SanitizeHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var sanitized = _htmlSanitizer.Sanitize(html);
            return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized;
        }

        private static HtmlSanitizer BuildSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.Add("http");
            sanitizer.AllowedSchemes.Add("https");

            sanitizer.AllowedTags.UnionWith(new[]
            {
                "p", "br", "strong", "b", "em", "i", "u", "s", "ol", "ul", "li", "blockquote", "a", "img", "h1", "h2", "h3", "span"
            });

            sanitizer.AllowedAttributes.UnionWith(new[]
            {
                "href", "target", "rel", "title", "src", "alt", "width", "height", "class", "style"
            });

            sanitizer.AllowedClasses.UnionWith(new[]
            {
                "ql-align-center", "ql-align-right", "ql-align-justify",
                "ql-size-large", "ql-size-small", "ql-size-huge",
                "ql-indent-1", "ql-indent-2", "ql-indent-3", "ql-indent-4", "ql-indent-5",
                "ql-direction-rtl"
            });

            sanitizer.AllowedCssProperties.UnionWith(new[]
            {
                "color", "background-color", "font-size", "text-align", "font-weight", "font-style", "text-decoration"
            });

            return sanitizer;
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
                Categories = (task.Categories != null && task.Categories.Any()
                    ? task.Categories
                    : string.IsNullOrWhiteSpace(task.Category) ? Array.Empty<string>() : new[] { task.Category }),
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
                IsAllDay = task.IsAllDay,
                RecurrenceGroupId = task.RecurrenceGroupId,
                Recurrence = ToRecurrenceDto(task.Recurrence),
                Assignees = task.Assignees?.Select(a => new TodoAssigneeDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    AvatarUrl = a.AvatarUrl
                }).ToList(),
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
