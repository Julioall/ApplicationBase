using Application.Domain.Model.Todo;
using Application.Domain.Model.Todo.Dtos;
using FluentValidation;

namespace Application.Domain.Validation.Todo
{
    public class TodoTaskValidator : AbstractValidator<TodoTask>
    {
        public TodoTaskValidator()
        {
            RuleFor(t => t.Title)
                .NotEmpty()
                .WithMessage("TodoTitleRequired");

            RuleFor(t => t.Status)
                .Must(status => Enum.IsDefined(typeof(TodoStatus), status))
                .WithMessage("TodoStatusInvalid");

            RuleFor(t => t.Priority)
                .InclusiveBetween(1, 3)
                .When(t => t.Priority.HasValue)
                .WithMessage("TodoPriorityInvalid");

            RuleForEach(t => t.Categories)
                .Must(cat => !string.IsNullOrWhiteSpace(cat))
                .WithMessage("TodoCategoryInvalid");

            RuleForEach(t => t.Assignees)
                .SetValidator(new TodoAssigneeValidator());

            RuleFor(t => t)
                .Must(HaveValidDates)
                .WithMessage("TodoDateRangeInvalid");

            RuleFor(t => t.Recurrence)
                .SetValidator(new TodoRecurrenceValidator())
                .When(t => t.Recurrence != null);

            When(t => !string.IsNullOrWhiteSpace(t.ContextId), () =>
            {
                RuleFor(t => t.ContextType)
                    .NotEmpty()
                    .WithMessage("TodoContextTypeRequired");
            });

            When(t => !string.IsNullOrWhiteSpace(t.ContextType), () =>
            {
                RuleFor(t => t.ContextId)
                    .NotEmpty()
                    .WithMessage("TodoContextIdRequired");
            });

            RuleForEach(t => t.Steps)
                .SetValidator(new TodoStepValidator());
        }

        private static bool HaveValidDates(TodoTask task)
        {
            if (task.StartDate.HasValue && task.DueDate.HasValue)
            {
                if (task.StartDate.Value > task.DueDate.Value)
                {
                    return false;
                }
            }

            if (task.Recurrence?.EndsOn.HasValue == true)
            {
                var anchor = task.StartDate ?? task.DueDate;
                if (anchor.HasValue && task.Recurrence.EndsOn.Value.Date < anchor.Value.Date)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class TodoStepValidator : AbstractValidator<TodoStep>
    {
        public TodoStepValidator()
        {
            RuleFor(s => s.Id)
                .NotEmpty()
                .WithMessage("TodoStepIdRequired");

            RuleFor(s => s.Title)
                .NotEmpty()
                .WithMessage("TodoStepTitleRequired");

            RuleFor(s => s.Order)
                .GreaterThanOrEqualTo(0)
                .WithMessage("TodoStepOrderInvalid");
        }
    }

    public class CreateTodoTaskDtoValidator : AbstractValidator<CreateTodoTaskDto>
    {
        public CreateTodoTaskDtoValidator()
        {
            RuleFor(t => t.Title)
                .NotEmpty()
                .WithMessage("TodoTitleRequired");

            RuleFor(t => t.Status)
                .Must(status => status == null || Enum.IsDefined(typeof(TodoStatus), status))
                .WithMessage("TodoStatusInvalid");

            RuleFor(t => t.Priority)
                .InclusiveBetween(1, 3)
                .When(t => t.Priority.HasValue)
                .WithMessage("TodoPriorityInvalid");

            RuleForEach(t => t.Categories)
                .Must(cat => string.IsNullOrWhiteSpace(cat) == false)
                .WithMessage("TodoCategoryInvalid");

            RuleForEach(t => t.Assignees)
                .SetValidator(new TodoAssigneeDtoValidator());

            RuleFor(t => t)
                .Must(HaveValidDates)
                .WithMessage("TodoDateRangeInvalid");

            RuleFor(t => t.Recurrence)
                .SetValidator(new TodoRecurrenceDtoValidator())
                .When(t => t.Recurrence != null);

            When(t => !string.IsNullOrWhiteSpace(t.ContextId), () =>
            {
                RuleFor(t => t.ContextType)
                    .NotEmpty()
                    .WithMessage("TodoContextTypeRequired");
            });

            When(t => !string.IsNullOrWhiteSpace(t.ContextType), () =>
            {
                RuleFor(t => t.ContextId)
                    .NotEmpty()
                    .WithMessage("TodoContextIdRequired");
            });

            RuleForEach(t => t.Steps)
                .SetValidator(new CreateTodoStepDtoValidator());
        }

        private static bool HaveValidDates(CreateTodoTaskDto task)
        {
            if (task.StartDate.HasValue && task.DueDate.HasValue)
            {
                if (task.StartDate.Value > task.DueDate.Value)
                {
                    return false;
                }
            }

            if (task.Recurrence?.EndsOn.HasValue == true)
            {
                var anchor = task.StartDate ?? task.DueDate;
                if (anchor.HasValue && task.Recurrence.EndsOn.Value.Date < anchor.Value.Date)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class UpdateTodoTaskDtoValidator : AbstractValidator<UpdateTodoTaskDto>
    {
        public UpdateTodoTaskDtoValidator()
        {
            RuleFor(t => t.Title)
                .NotEmpty()
                .WithMessage("TodoTitleRequired")
                .When(t => t.Title != null);

            RuleFor(t => t.Status)
                .Must(status => status == null || Enum.IsDefined(typeof(TodoStatus), status))
                .WithMessage("TodoStatusInvalid");

            RuleFor(t => t.Priority)
                .InclusiveBetween(1, 3)
                .When(t => t.Priority.HasValue)
                .WithMessage("TodoPriorityInvalid");

            RuleForEach(t => t.Categories)
                .Must(cat => string.IsNullOrWhiteSpace(cat) == false)
                .WithMessage("TodoCategoryInvalid");

            RuleForEach(t => t.Assignees)
                .SetValidator(new TodoAssigneeDtoValidator());

            RuleFor(t => t)
                .Must(HaveValidDates)
                .WithMessage("TodoDateRangeInvalid");

            RuleFor(t => t.Recurrence)
                .SetValidator(new TodoRecurrenceDtoValidator())
                .When(t => t.Recurrence != null);

            When(t => !string.IsNullOrWhiteSpace(t.ContextId), () =>
            {
                RuleFor(t => t.ContextType)
                    .NotEmpty()
                    .WithMessage("TodoContextTypeRequired");
            });

            When(t => !string.IsNullOrWhiteSpace(t.ContextType), () =>
            {
                RuleFor(t => t.ContextId)
                    .NotEmpty()
                    .WithMessage("TodoContextIdRequired");
            });
        }

        private static bool HaveValidDates(UpdateTodoTaskDto task)
        {
            if (task.StartDate.HasValue && task.DueDate.HasValue)
            {
                if (task.StartDate.Value > task.DueDate.Value)
                {
                    return false;
                }
            }

            if (task.Recurrence?.EndsOn.HasValue == true)
            {
                var anchor = task.StartDate ?? task.DueDate;
                if (anchor.HasValue && task.Recurrence.EndsOn.Value.Date < anchor.Value.Date)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class CreateTodoStepDtoValidator : AbstractValidator<CreateTodoStepDto>
    {
        public CreateTodoStepDtoValidator()
        {
            RuleFor(s => s.Title)
                .NotEmpty()
                .WithMessage("TodoStepTitleRequired");
        }
    }

    public class UpdateTodoStepDtoValidator : AbstractValidator<UpdateTodoStepDto>
    {
        public UpdateTodoStepDtoValidator()
        {
            RuleFor(s => s.Title)
                .Must(title => title == null || !string.IsNullOrWhiteSpace(title))
                .WithMessage("TodoStepTitleRequired");
        }
    }

    public class TodoAssigneeValidator : AbstractValidator<TodoAssignee>
    {
        public TodoAssigneeValidator()
        {
            RuleFor(a => a.Name)
                .NotEmpty()
                .WithMessage("TodoAssigneeNameRequired");
        }
    }

    public class TodoAssigneeDtoValidator : AbstractValidator<TodoAssigneeDto>
    {
        public TodoAssigneeDtoValidator()
        {
            RuleFor(a => a.Name)
                .NotEmpty()
                .WithMessage("TodoAssigneeNameRequired");
        }
    }

    public class TodoRecurrenceValidator : AbstractValidator<TodoRecurrence>
    {
        public TodoRecurrenceValidator()
        {
            RuleFor(r => r.Type)
                .IsInEnum()
                .WithMessage("TodoRecurrenceInvalid");

            RuleFor(r => r.Interval)
                .GreaterThanOrEqualTo(1)
                .WithMessage("TodoRecurrenceInvalid");

            RuleFor(r => r.DaysOfWeek)
                .NotEmpty()
                .WithMessage("TodoRecurrenceInvalid")
                .When(r => r.Type == TodoRecurrenceType.Weekly);
        }
    }

    public class TodoRecurrenceDtoValidator : AbstractValidator<TodoRecurrenceDto>
    {
        public TodoRecurrenceDtoValidator()
        {
            RuleFor(r => r.Type)
                .IsInEnum()
                .WithMessage("TodoRecurrenceInvalid");

            RuleFor(r => r.Interval)
                .GreaterThanOrEqualTo(1)
                .WithMessage("TodoRecurrenceInvalid");

            RuleFor(r => r.DaysOfWeek)
                .NotEmpty()
                .WithMessage("TodoRecurrenceInvalid")
                .When(r => r.Type == TodoRecurrenceType.Weekly);
        }
    }

    public class ReorderTodoStepsDtoValidator : AbstractValidator<ReorderTodoStepsDto>
    {
        public ReorderTodoStepsDtoValidator()
        {
            RuleFor(x => x.StepIds)
                .NotNull()
                .WithMessage("TodoStepOrderInvalid");

            RuleFor(x => x.StepIds)
                .Must(ids => ids != null && ids.Any())
                .WithMessage("TodoStepOrderInvalid");

            RuleFor(x => x.StepIds)
                .Must(ids => ids == null || ids.Count() == ids.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                .WithMessage("TodoStepOrderInvalid");
        }
    }
}
