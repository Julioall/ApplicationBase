using Application.Domain.Model.User;
using Application.Service.Interface;
using Application.Service.Service;
using Application.Tests.Setup;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Application.Tests.Services
{
    public class UserServiceTests : BaseTest
    {
        private readonly IUserService _userService;

        public UserServiceTests()
        {
            _userService = _serviceProvider.GetService<IUserService>()
                ?? throw new Exception($"{nameof(IUserService)} não foi encontrado");
        }

        [Fact]
        public void GetAll_ShouldReturnListOfUsers()
        {
            // Arrange
            const string email1 = "sam@santos.com";
            const string email2 = "samuel@santos.com";
            const string email3 = "aaa@bbb.com";

            var users = new List<User>
            {
                CreateValidUser(email1),
                CreateValidUser(email2),
                CreateValidUser(email3),
            };

            users.ForEach(_session.Store);
            _session.SaveChanges();

            // Act
            var list = _userService.GetAllAsync();

            // Assert
            Assert.NotNull(list);
            Assert.Equal(3, list.Result.Count());
            Assert.Contains(list.Result, u => u.Account.Email == email1);
            Assert.Collection(list.Result,
                u => Assert.Equal(email1, u.Account.Email),
                u => Assert.Equal(email2, u.Account.Email),
                u => Assert.Equal(email3, u.Account.Email));

        }

        private static User CreateValidUser(string email)
        {
            return new User
            {
                Account = new UserAccount
                {
                    Email = email,
                    Password = "Valid123!",
                    Role = "User"
                },
                Profile = new UserProfile
                {
                    Name = "Valid Name",
                },
            };
        }
    }
}
