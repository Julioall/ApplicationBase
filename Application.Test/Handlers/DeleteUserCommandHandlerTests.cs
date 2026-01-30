using Application.Service.Handlers;
using Application.Service.Interface;
using Moq;
using Xunit;

namespace Application.Tests.Handlers
{
    /// <summary>
    /// Testes para DeleteUserCommandHandler.
    /// Valida a deleção de usuários através do padrão CQRS/MediatR.
    /// </summary>
    public class DeleteUserCommandHandlerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly DeleteUserCommandHandler _handler;

        public DeleteUserCommandHandlerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _handler = new DeleteUserCommandHandler(_userServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidUserId_CallsUserServiceDeleteAsync()
        {
            // Arrange
            var userId = "user-123";
            var command = new DeleteUserCommand(userId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _userServiceMock.Verify(x => x.DeleteAsync(userId), Times.Once);
        }

        [Fact]
        public async Task Handle_WithDifferentUserIds_DeletesCorrectUsers()
        {
            // Arrange
            var userIds = new[] { "user-1", "user-2", "user-3" };

            // Act
            foreach (var userId in userIds)
            {
                var command = new DeleteUserCommand(userId);
                await _handler.Handle(command, CancellationToken.None);
            }

            // Assert
            foreach (var userId in userIds)
            {
                _userServiceMock.Verify(x => x.DeleteAsync(userId), Times.Once);
            }
        }
    }
}
