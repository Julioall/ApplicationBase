using Application.Domain.Interface.Todo;
using Application.Domain.Model.Todo;
using Application.Infrastructure.Indexes;
using Application.Infrastructure.Service;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System.Linq;

namespace Application.Infrastructure.Repository.Todo
{
    public class TodoRepository : ITodoRepository
    {
        private readonly IServiceRavenDB _serviceRavenDb;

        public TodoRepository(IServiceRavenDB serviceRavenDb)
        {
            _serviceRavenDb = serviceRavenDb;
        }

        public async Task AddAsync(TodoTask entity)
        {
            await _serviceRavenDb.AsyncSession.StoreAsync(entity);
        }

        public async Task<TodoTask?> GetByIdAsync(string id)
        {
            return await _serviceRavenDb.AsyncSession.LoadAsync<TodoTask>(id);
        }

        public async Task<IReadOnlyList<TodoTask>> SearchAsync(TodoTaskSearchQuery query)
        {
            query ??= new TodoTaskSearchQuery();

            var ravenQuery = _serviceRavenDb.AsyncSession.Query<TodoTask, TodoTasks_ByContextAndStatus>();

            if (!query.IncludeArchived)
            {
                ravenQuery = ravenQuery.Where(t => !t.IsArchived);
            }

            if (!string.IsNullOrWhiteSpace(query.ContextType))
            {
                ravenQuery = ravenQuery.Where(t => t.ContextType == query.ContextType);
            }

            if (!string.IsNullOrWhiteSpace(query.ContextId))
            {
                ravenQuery = ravenQuery.Where(t => t.ContextId == query.ContextId);
            }

            if (!string.IsNullOrWhiteSpace(query.AssignedToUserId))
            {
                ravenQuery = ravenQuery.Where(t => t.AssignedToUserId == query.AssignedToUserId);
            }

            ravenQuery = ravenQuery
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.CreatedAt);

            return await ravenQuery.ToListAsync();
        }

        public async Task UpdateAsync(TodoTask entity)
        {
            await _serviceRavenDb.AsyncSession.StoreAsync(entity);
        }
    }
}
