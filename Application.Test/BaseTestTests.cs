using Application.Domain.Interface;
using Application.Tests.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests
{
    public class BaseTestTests : BaseTest
    {
        [Fact]
        public void Should_Initialize_ServiceCollection()
        {
            Assert.NotNull(_serviceCollection);
        }

        [Fact]
        public void Should_Initialize_ServiceProvider()
        {
            Assert.NotNull(_serviceProvider);
        }

        [Fact]
        public void Should_Register_IServiceRavenDB()
        {
            var service = _serviceProvider.GetService<IServiceRavenDB>();

            Assert.NotNull(service);
            Assert.NotNull(service.Store);
            Assert.NotNull(service.Session);
        }
    }
}