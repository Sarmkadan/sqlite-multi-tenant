using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SqliteMultiTenant.Configuration;
using SqliteMultiTenant.Utilities;
using Xunit;
using FluentAssertions;

namespace SqliteMultiTenant.Tests
{
    public sealed class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddSqliteMultiTenantServices_RegistersRequiredServices()
        {
            var services = new ServiceCollection();
            
            services.AddSqliteMultiTenantServices();
            
            services.Should().Contain(s => s.ServiceType == typeof(IConfigurationManager));
            services.Should().Contain(s => s.ServiceType == typeof(IDataMapper));
        }

        [Fact]
        public void AddSqliteMultiTenantServices_ThrowsArgumentNullException_WhenServicesNull()
        {
            IServiceCollection? services = null;
            Action act = () => services!.AddSqliteMultiTenantServices();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddExceptionHandling_RegistersServices()
        {
            var services = new ServiceCollection();
            services.AddExceptionHandling();
            services.Should().Contain(s => s.ServiceType == typeof(SqliteMultiTenant.Exceptions.IExceptionProcessor));
        }

        [Fact]
        public void AddExceptionHandling_ThrowsArgumentNullException_WhenServicesNull()
        {
            IServiceCollection? services = null;
            Action act = () => services!.AddExceptionHandling();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddEventHandlers_RegistersServices()
        {
            var services = new ServiceCollection();
            services.AddEventHandlers();
            services.Should().Contain(s => s.ServiceType == typeof(SqliteMultiTenant.Events.IDomainEventHandler<SqliteMultiTenant.Events.TenantCreatedNotificationEvent>));
        }

        [Fact]
        public void AddEventHandlers_ThrowsArgumentNullException_WhenServicesNull()
        {
            IServiceCollection? services = null;
            Action act = () => services!.AddEventHandlers();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddHealthChecks_RegistersServices()
        {
            var services = new ServiceCollection();
            SqliteMultiTenant.Configuration.ServiceCollectionExtensions.AddHealthChecks(services);
            services.Should().Contain(s => s.ServiceType == typeof(SqliteMultiTenant.Health.HealthCheckService));
        }

        [Fact]
        public void AddFormatters_RegistersServices()
        {
            var services = new ServiceCollection();
            services.AddFormatters();
            services.Should().Contain(s => s.ServiceType == typeof(SqliteMultiTenant.Formatters.OutputFormatter));
        }

        [Fact]
        public void UseRequestResponseLogging_RegistersMiddleware()
        {
            var app = Substitute.For<IApplicationBuilder>();
            app.UseRequestResponseLogging();
            // Verify Use was called, as it's the only way to register middleware
            app.Received(2).Use(Arg.Any<Func<Microsoft.AspNetCore.Http.RequestDelegate, Microsoft.AspNetCore.Http.RequestDelegate>>());
        }

        [Fact]
        public void UseRequestResponseLogging_ThrowsArgumentNullException_WhenAppNull()
        {
            IApplicationBuilder? app = null;
            Action act = () => app!.UseRequestResponseLogging();
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
