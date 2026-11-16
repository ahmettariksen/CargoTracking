CargoTracking – Cargo Management and Tracking API

A backend cargo management and tracking system built with ASP.NET Core 9, following Clean Architecture and CQRS principles.

CargoTracking provides shipment management, courier assignment, warehouse operations, shipment tracking, and asynchronous background processing using Redis, RabbitMQ, and the Outbox Pattern.

Project Overview

The main goal of this project is to develop a scalable and maintainable backend application that demonstrates real-world .NET backend development practices.

The application supports different user roles, manages shipment lifecycles, assigns couriers based on geographical proximity, and tracks shipment movements between warehouses and delivery locations.

Technologies

Backend Development

Technology

Version

Purpose

C#

-

Main programming language

.NET

9

Application platform

ASP.NET Core Web API

9

RESTful API development

Entity Framework Core

9.0.20

ORM and database operations

SQL Server

-

Relational database

MediatR

14.2.0

CQRS request and handler communication

FluentValidation

12.2.0

Request and command validation

Swagger / OpenAPI

-

API documentation

Authentication and Security

JWT Bearer Authentication

Refresh Token mechanism

Role-based Authorization

Password Hashing with ASP.NET Core Identity's PasswordHasher

Centralized Exception Handling

Distributed Systems and Infrastructure

Technology

Version

Purpose

Redis

-

Geospatial operations and proximity-based queries

StackExchange.Redis

3.4.5

Redis integration for .NET

RabbitMQ

4

Asynchronous message broker

RabbitMQ.Client

7.2.2

RabbitMQ integration

MailKit

4.18.2

Email delivery

Docker

-

Application containerization

Docker Compose

-

Container configuration

GitHub Container Registry

-

Docker image storage

Database and Health Monitoring

Technology

Version

Purpose

Microsoft.EntityFrameworkCore.SqlServer

9.0.20

SQL Server provider

EF Core Migrations

9.0.20

Database schema management

AspNetCore.HealthChecks.Redis

9.0.0

Redis health monitoring

EF Core Health Checks

9.0.23

Database health monitoring

Testing and CI/CD

Technology

Version

Purpose

xUnit

2.9.2

Automated testing

Moq

4.21.0

Mocking dependencies

EF Core InMemory

9.0.20

In-memory database testing

Microsoft.NET.Test.Sdk

17.12.0

Test execution

Coverlet Collector

6.0.2

Code coverage collection

GitHub Actions

-

CI/CD automation

Architecture and Design Patterns

Clean Architecture

CQRS

Dependency Injection

Outbox Pattern

Background Services

Optimistic Concurrency Control

Manual Mapping

Architecture

CargoTracking follows Clean Architecture principles to separate business logic, application use cases, infrastructure concerns, and HTTP communication.

The solution consists of five projects:

CargoTracking/
│
├── CargoTracking.API/
│   ├── Configuration/
│   ├── Contracts/
│   ├── Controllers/
│   ├── Exceptions/
│   ├── Initialization/
│   ├── Services/
│   ├── Swagger/
│   ├── Dockerfile
│   └── Program.cs
│
├── CargoTracking.Application/
│   ├── Abstractions/
│   ├── Behaviors/
│   ├── Events/
│   ├── Exceptions/
│   ├── Features/
│   └── Interfaces/
│
├── CargoTracking.Domain/
│   ├── Common/
│   ├── Entities/
│   └── Enums/
│
├── CargoTracking.Infrastructure/
│   ├── BackgroundJobs/
│   ├── Messaging/
│   ├── Migrations/
│   ├── Options/
│   ├── Persistence/
│   ├── Redis/
│   ├── Services/
│   └── Settings/
│
└── CargoTracking.Tests/

Layer Responsibilities

Domain Layer

Contains the core business entities, base entity, and enumerations. It represents business rules independently of infrastructure technologies.

Application Layer

Contains application use cases organized around features using CQRS.

Commands handle operations that change application state.

Queries retrieve application data.

MediatR dispatches requests to their respective handlers.

FluentValidation validates commands.

Application abstractions define contracts for infrastructure services.

Infrastructure Layer

Implements external dependencies and technical concerns.

Entity Framework Core and SQL Server persistence

Database configurations and migrations

Redis geospatial operations

RabbitMQ message publishing and consumers

Outbox background processing

JWT and Refresh Token services

SMTP email delivery

API Layer

Provides the HTTP interface to the application.

Controllers and request contracts

JWT authentication and authorization

Current user service

Global exception handling

Swagger configuration

Dependency Injection and service registration

Admin initialization

Tests Layer

Contains automated tests for application behavior, database operations, concurrency, and infrastructure-related scenarios.

Core Features

User and Authentication Management

User registration and login

JWT-based authentication

Refresh Token management

Role-based access control

Password hashing

Shipment Management

Create and manage shipments

Update shipment statuses

Track shipment history

Deliver or fail deliveries

Process shipment returns

Courier Management

Create courier profiles

Update courier locations

Manage courier availability

Find nearby available couriers using Redis Geospatial

Automatically assign couriers to shipments

Warehouse Management

Create warehouses

Select the nearest warehouse

Receive shipments at warehouses

Assign delivery couriers

Transfer shipments between warehouses

Shipment Tracking

Shipment tracking events are recorded to maintain a history of shipment movements and status changes.

Background Processing and Messaging

Outbox Pattern for reliable message processing

Background service for processing pending Outbox messages

Retry mechanism with exponential backoff

Message publishing through RabbitMQ

RabbitMQ consumers for shipment and courier assignment events

Email notifications through SMTP

Concurrency Management

SQL Server RowVersion is used for optimistic concurrency control on shipment and courier profile records.

This helps prevent conflicting updates when multiple operations attempt to modify the same data concurrently.

Health Monitoring

Health checks are configured to monitor SQL Server and Redis connectivity.

Shipment Lifecycle

A shipment moves through different statuses during its lifecycle.

Created
   |
   v
Accepted
   |
   v
PickedUpFromSender
   |
   v
AtWarehouse
   |
   v
AssignedToCourier
   |
   v
PickedUp
   |
   v
InTransit
   |
   v
Delivered

Additional flows support delivery failures, returns, warehouse transfers, and cancellations according to the shipment's current status.

Testing

The project includes automated tests using xUnit, Moq, and Entity Framework Core InMemory.

Test coverage includes:

Application and business logic

Command and query handlers

Validation rules

Database operations

Shipment and courier concurrency scenarios

Outbox processing

Latest verified test result: 105 passed, 0 failed.

Run the tests with:

dotnet test CargoTracking.sln

Docker and Deployment

The API is containerized using Docker and configured through Docker Compose.

The published API image is available on GitHub Container Registry:

ghcr.io/ahmettariksen/cargotracking-api:latest

Start the API container:

docker compose up -d

Check the running containers:

docker compose ps

The current Docker Compose configuration runs the API container. SQL Server, Redis, and RabbitMQ must be available separately and configured with the appropriate connection settings.

CI/CD Pipeline

GitHub Actions automates the build, test, and Docker image publishing process.

The pipeline includes:

Restoring NuGet dependencies.

Building the solution.

Running automated tests.

Building the Docker image.

Publishing the image to GitHub Container Registry when changes are pushed to the main branch.

This ensures that the application is built and tested before a new Docker image is published.

Getting Started

Prerequisites

.NET 9 SDK

SQL Server

Redis

RabbitMQ

Docker Desktop (for containerized execution)

Installation

Clone the repository:

git clone https://github.com/ahmettariksen/CargoTracking.git
cd CargoTracking

Configure the required application settings, including:

SQL Server connection string

Redis connection

RabbitMQ connection

JWT settings

SMTP email settings

Keep sensitive configuration values in local configuration or environment variables. Do not commit passwords, tokens, or production connection strings.

Restore dependencies:

dotnet restore

Apply database migrations:

dotnet ef database update --project CargoTracking.Infrastructure --startup-project CargoTracking.API

Run the API:

dotnet run --project CargoTracking.API

API Documentation

After starting the application, Swagger UI is available at:

http://localhost:8080/swagger

Swagger provides an interactive interface for exploring and testing the API endpoints.

Repository

GitHub: https://github.com/ahmettariksen/CargoTracking

Author

Ahmet Tarık Şen

.NET Backend Developer
