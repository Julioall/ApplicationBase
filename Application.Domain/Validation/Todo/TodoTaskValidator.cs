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

            RuleFor(t => t)
                .Must(HaveValidDates)
                .WithMessage("TodoDateRangeInvalid");

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
                return task.StartDate.Value.Date <= task.DueDate.Value.Date;
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

            RuleFor(t => t)
                .Must(HaveValidDates)
                .WithMessage("TodoDateRangeInvalid");

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
                return task.StartDate.Value.Date <= task.DueDate.Value.Date;
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

            RuleFor(t => t)
                .Must(HaveValidDates)
                .WithMessage("TodoDateRangeInvalid");

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
                return task.StartDate.Value.Date <= task.DueDate.Value.Date;
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
