using Application.Domain.CQRS;

namespace Application.Service.CQRS.Queries
{
    /// <summary>
    /// Query para buscar usuário por email.
    /// </summary>
    public record GetUserByEmailQuery(string Email) : IQuery<GetUserByEmailResponse?>;

    /// <summary>
    /// Response da GetUserByEmailQuery.
    /// </summary>
    public record GetUserByEmailResponse(
        string UserId,
        string Email,
        string Username,
        string FullName,
        DateTime CreatedAt
    );
}
