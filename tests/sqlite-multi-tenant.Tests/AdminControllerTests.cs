using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Api.Controllers;
using SqliteMultiTenant.Api.Responses;
using SqliteMultiTenant.Health;
using SqliteMultiTenant.Monitoring;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public sealed class AdminControllerTests
    {
        private readonly HealthCheckService _mockHealthCheck;
        private readonly MetricsService _mockMetricsService;
        private readonly ILogger<AdminController> _mockLogger;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            var healthLogger = Substitute.For<ILogger<HealthCheckService>>();
            _mockHealthCheck = new HealthCheckService(healthLogger);
            
            var metricsLogger = Substitute.For<ILogger<MetricsService>>();
            _mockMetricsService = new MetricsService(metricsLogger);
            
            _mockLogger = Substitute.For<ILogger<AdminController>>();
            _controller = new AdminController(_mockHealthCheck, _mockMetricsService, _mockLogger);
        }

        [Fact]
        public void GetMetricsDashboard_ReturnsOkResult_WithMetricsSnapshot()
        {
            // Arrange
            _mockMetricsService.RecordRequest("/api/test", 150, 200);
            _mockMetricsService.RecordBackup(1024, 500, true);

            // Act
            var result = _controller.GetMetricsDashboard() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            
            var response = result.Value as ApiResponse<MetricsSnapshot>;
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Data);
            Assert.Equal(1, response.Data.TotalRequests);
            Assert.Equal(1, response.Data.TotalBackups);
            Assert.Equal(1024, response.Data.TotalBackupBytes);
        }

        [Fact]
        public void GetMetricsDashboard_ReturnsEmptySnapshot_WhenNoMetricsRecorded()
        {
            // Act
            var result = _controller.GetMetricsDashboard() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            
            var response = result.Value as ApiResponse<MetricsSnapshot>;
            Assert.NotNull(response);
            Assert.True(response.IsSuccess);
            Assert.NotNull(response.Data);
            Assert.Equal(0, response.Data.TotalRequests);
        }
    }
}