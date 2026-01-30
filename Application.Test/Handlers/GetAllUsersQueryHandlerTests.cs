using Application.Service.Handlers;
using Application.Service.Interface;
using Moq;
using Xunit;

namespace Application.Tests.Handlers
{
    /// <summary>
    /// Testes para GetAllUsersQueryHandler.
    /// Valida a recuperação de todos os usuários através do padrão CQRS/MediatR.
    /// </summary>
    public class GetAllUsersQueryHandlerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly GetAllUsersQueryHandler _handler;

        public GetAllUsersQueryHandlerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _handler = new GetAllUsersQueryHandler(_userServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidQuery_ReturnsAllUsers()
        {
            // Arrange
            var users = new[]
            {
                new Domain.Model.User.User { Id = "1", Account = new Domain.Model.User.UserAccount { Email = "user1@test.com" } },
                new Domain.Model.User.User { Id = "2", Account = new Domain.Model.User.UserAccount { Email = "user2@test.com" } }
            };

            _userServiceMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(users);

            var query = new GetAllUsersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _userServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenNoUsersExist_ReturnsEmptyEnumerable()
        {
            // Arrange
            _userServiceMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(Enumerable.Empty<Domain.Model.User.User>());

            var query = new GetAllUsersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }
    }
}
