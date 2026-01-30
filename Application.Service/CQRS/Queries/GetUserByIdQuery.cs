using Application.Domain.CQRS;

namespace Application.Service.CQRS.Queries
{
    /// <summary>
    /// Query para buscar usuário por ID.
    /// </summary>
    public record GetUserByIdQuery(string UserId) : IQuery<GetUserByIdResponse>;

    /// <summary>
    /// Response da GetUserByIdQuery.
    /// </summary>
    public record GetUserByIdResponse(
        string UserId,
        string Email,
        string Username,
        string FullName,
        string? PhoneNumber,
        string? Address,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}
