using Application.Domain.Model.Todo;
using Raven.Client.Documents.Indexes;

namespace Application.Infrastructure.Indexes
{
    public class TodoTasks_ByContextAndStatus : AbstractIndexCreationTask<TodoTask>
    {
        public TodoTasks_ByContextAndStatus()
        {
            Map = tasks => from t in tasks
                           select new
                           {
                               t.ContextType,
                               t.ContextId,
                               t.Status,
                               t.AssignedToUserId,
                               t.IsArchived,
                               t.Priority,
                               t.DueDate,
                               t.CreatedAt
                           };
        }
    }
}
