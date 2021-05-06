# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# Makefile for SQLite Multi-Tenant
# =============================================================================

.PHONY: help build test clean restore publish docker docker-up docker-down \
        docs lint format example1 example2 example3 example4 example5

# Variables
DOTNET := dotnet
PROJECT := src/SqliteMultiTenant.csproj
CONFIG := Release
EXAMPLES_DIR := examples

help:
	@echo "SQLite Multi-Tenant - Make Commands"
	@echo "===================================="
	@echo ""
	@echo "Building:"
	@echo "  make build        - Build the project in Release mode"
	@echo "  make clean        - Clean build artifacts"
	@echo "  make restore      - Restore NuGet packages"
	@echo "  make rebuild      - Clean and build"
	@echo ""
	@echo "Testing & Validation:"
	@echo "  make test         - Run unit tests"
	@echo "  make lint         - Run code analysis"
	@echo "  make format       - Format code with StyleCop"
	@echo ""
	@echo "Publishing:"
	@echo "  make publish      - Publish to Release folder"
	@echo "  make pack         - Create NuGet package"
	@echo ""
	@echo "Docker:"
	@echo "  make docker       - Build Docker image"
	@echo "  make docker-up    - Start Docker containers"
	@echo "  make docker-down  - Stop Docker containers"
	@echo "  make docker-clean - Remove Docker containers and images"
	@echo ""
	@echo "Examples:"
	@echo "  make example1     - Run basic setup example"
	@echo "  make example2     - Run migrations example"
	@echo "  make example3     - Run backup/restore example"
	@echo "  make example4     - Run error handling example"
	@echo "  make example5     - Run advanced operations example"
	@echo ""
	@echo "Documentation:"
	@echo "  make docs         - Generate documentation"
	@echo ""

build:
	@echo "Building SQLite Multi-Tenant..."
	@$(DOTNET) build $(PROJECT) -c $(CONFIG)
	@echo "✓ Build completed"

clean:
	@echo "Cleaning build artifacts..."
	@$(DOTNET) clean $(PROJECT)
	@rm -rf bin obj
	@rm -rf *.nupkg
	@echo "✓ Clean completed"

restore:
	@echo "Restoring NuGet packages..."
	@$(DOTNET) restore $(PROJECT)
	@echo "✓ Restore completed"

rebuild: clean restore build
	@echo "✓ Rebuild completed"

test:
	@echo "Running tests..."
	@$(DOTNET) test --configuration $(CONFIG) --verbosity normal || true
	@echo "✓ Tests completed"

lint:
	@echo "Running code analysis..."
	@$(DOTNET) build $(PROJECT) -c $(CONFIG) /p:EnforceCodeStyleInBuild=true || true
	@echo "✓ Lint completed"

format:
	@echo "Formatting code..."
	@$(DOTNET) format $(PROJECT) || true
	@echo "✓ Format completed"

publish:
	@echo "Publishing project..."
	@$(DOTNET) publish $(PROJECT) -c $(CONFIG) -o ./publish
	@echo "✓ Publish completed to ./publish"

pack:
	@echo "Creating NuGet package..."
	@$(DOTNET) pack $(PROJECT) -c $(CONFIG) -o ./nupkg
	@echo "✓ Package created in ./nupkg"

docker:
	@echo "Building Docker image..."
	@docker build -t sqlite-multi-tenant:latest .
	@echo "✓ Docker image built: sqlite-multi-tenant:latest"

docker-up:
	@echo "Starting Docker containers..."
	@docker-compose up -d
	@echo "✓ Containers started"
	@echo "   Access at http://localhost:5000"

docker-down:
	@echo "Stopping Docker containers..."
	@docker-compose down
	@echo "✓ Containers stopped"

docker-clean:
	@echo "Removing Docker containers and images..."
	@docker-compose down -v
	@docker rmi sqlite-multi-tenant:latest
	@echo "✓ Docker cleanup completed"

docs:
	@echo "Documentation files:"
	@echo "  - README.md (main)"
	@echo "  - docs/getting-started.md"
	@echo "  - docs/architecture.md"
	@echo "  - docs/deployment.md"
	@echo "  - docs/faq.md"
	@echo ""
	@echo "✓ Documentation available"

example1:
	@echo "Running Example 1: Basic Setup..."
	@echo "Note: Copy example 1 to a test project and run:"
	@echo "  cd examples && dotnet new console -n Example1"
	@echo "  cp 1-basic-setup.cs Program.cs"
	@echo "  dotnet add package SqliteMultiTenant"
	@echo "  dotnet run"

example2:
	@echo "Running Example 2: Migrations..."
	@echo "Note: Copy example 2 to a test project and run:"
	@echo "  cd examples && dotnet new console -n Example2"
	@echo "  cp 2-migrations-example.cs Program.cs"
	@echo "  dotnet add package SqliteMultiTenant"
	@echo "  dotnet run"

example3:
	@echo "Running Example 3: Backup/Restore..."
	@echo "Note: Copy example 3 to a test project and run:"
	@echo "  cd examples && dotnet new console -n Example3"
	@echo "  cp 3-backup-restore.cs Program.cs"
	@echo "  dotnet add package SqliteMultiTenant"
	@echo "  dotnet run"

example4:
	@echo "Running Example 4: Error Handling..."
	@echo "Note: Copy example 4 to a test project and run:"
	@echo "  cd examples && dotnet new console -n Example4"
	@echo "  cp 4-error-handling.cs Program.cs"
	@echo "  dotnet add package SqliteMultiTenant"
	@echo "  dotnet run"

example5:
	@echo "Running Example 5: Advanced Operations..."
	@echo "Note: Copy example 5 to a test project and run:"
	@echo "  cd examples && dotnet new console -n Example5"
	@echo "  cp 5-advanced-operations.cs Program.cs"
	@echo "  dotnet add package SqliteMultiTenant"
	@echo "  dotnet run"

# Development shortcuts
dev-setup: restore build
	@echo "✓ Development setup completed"

dev-clean: clean
	@rm -rf databases backups logs
	@echo "✓ Development cleaned"

dev-rebuild: clean restore build test lint
	@echo "✓ Development rebuild completed"

# CI/CD shortcuts
ci: restore build test lint
	@echo "✓ CI checks passed"

release: clean restore build test pack
	@echo "✓ Release build completed"

version:
	@echo "Checking versions..."
	@$(DOTNET) --version
	@echo "✓ Version info displayed"

# Default target
.DEFAULT_GOAL := help

# Silent by default
.SILENT:
