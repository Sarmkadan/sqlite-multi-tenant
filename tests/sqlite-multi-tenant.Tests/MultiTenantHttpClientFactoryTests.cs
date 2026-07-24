using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using SqliteMultiTenant.Integration;
using Xunit;

namespace SqliteMultiTenant.Tests
{
    public class MultiTenantHttpClientFactoryTests
    {
        [Fact]
        public void CreateClientForTenant_RejectsFileScheme()
        {
            var factory = new MultiTenantHttpClientFactory(new Microsoft.Extensions.Logging.Abstractions.NullLogger<MultiTenantHttpClientFactory>());
            
            Assert.Throws<ArgumentException>(() => 
                factory.CreateClientForTenant("tenant1", baseAddress: "file:///etc/passwd"));
        }

        [Fact]
        public void CreateClientForTenant_RejectsLinkLocalAddress()
        {
            var factory = new MultiTenantHttpClientFactory(new Microsoft.Extensions.Logging.Abstractions.NullLogger<MultiTenantHttpClientFactory>());
            
            Assert.Throws<ArgumentException>(() => 
                factory.CreateClientForTenant("tenant1", baseAddress: "http://169.254.169.254/"));
        }

        [Fact]
        public void CreateClientForTenant_AllowsAllowlistedHost()
        {
            var allowedHosts = new List<string> { "trusted-host.com" };
            var factory = new MultiTenantHttpClientFactory(
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<MultiTenantHttpClientFactory>(),
                allowedHosts: allowedHosts);
            
            // This would normally be rejected because of IP range or other checks, 
            // but if it were a private IP, it would still be allowed if it's on the allowlist.
            // Let's test with a simple http address
            var client = factory.CreateClientForTenant("tenant1", baseAddress: "http://trusted-host.com/");
            Assert.NotNull(client);
            Assert.Equal("http://trusted-host.com/", client.BaseAddress?.ToString());
        }

        [Fact]
        public void TenantHttpClientBuilder_RejectsFileScheme()
        {
            var builder = new TenantHttpClientBuilder();
            
            Assert.Throws<ArgumentException>(() => 
                builder.ForTenant("tenant1")
                       .WithBaseAddress("file:///etc/passwd")
                       .Build());
        }

        [Fact]
        public void TenantHttpClientBuilder_RejectsLinkLocalAddress()
        {
            var builder = new TenantHttpClientBuilder();
            
            Assert.Throws<ArgumentException>(() => 
                builder.ForTenant("tenant1")
                       .WithBaseAddress("http://169.254.169.254/")
                       .Build());
        }
    }
}
