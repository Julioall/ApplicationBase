using Application.Service.Handlers;
using Application.Service.Interface;
using Moq;
using Xunit;

namespace Application.Tests.Handlers
{
    /// <summary>
    /// Testes para GetUserByIdQueryHandler.
    /// Valida a recuperação de usuários por ID através do padrão CQRS/MediatR.
    /// </summary>
    public class GetUserByIdQueryHandlerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly GetUserByIdQueryHandler _handler;

        public GetUserByIdQueryHandlerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _handler = new GetUserByIdQueryHandler(_userServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidUserId_ReturnsUser()
        {
            // Arrange
            var userId = "user-123";
            var user = new Domain.Model.User.User
            {
                Id = userId,
                Account = new Domain.Model.User.UserAccount { Email = "test@example.com" }
            };

            _userServiceMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            var query = new GetUserByIdQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("test@example.com", result.Account.Email);
            _userServiceMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentUserId_ReturnsNull()
        {
            // Arrange
            var userId = "non-existent";
            _userServiceMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((Domain.Model.User.User?)null);

            var query = new GetUserByIdQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_CallsServiceWithCorrectUserId()
        {
            // Arrange
            var userId = "specific-user";
            _userServiceMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((Domain.Model.User.User?)null);

            var query = new GetUserByIdQuery(userId);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _userServiceMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }
    }
}
