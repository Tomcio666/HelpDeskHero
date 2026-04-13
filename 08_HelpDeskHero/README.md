# HelpDeskHero Minimal Implementation

This is a minimal implementation of a HelpDeskHero system with ticket management functionality.

## Features Implemented

- Ticket creation, retrieval, update, and deletion
- Ticket attachment handling
- Ticket comment functionality
- SLA calculation and monitoring
- Ticket assignment
- Audit logging
- Real-time notifications via SignalR
- Background job processing with Hangfire
- File storage for attachments

## Architecture

The implementation follows a clean architecture approach with:

- **Domain Layer**: Contains core business entities and interfaces
- **Application Layer**: Contains application services and interfaces
- **Infrastructure Layer**: Contains implementations of interfaces and infrastructure concerns
- **API Layer**: Contains controllers and web-specific concerns

## Controllers

- `TicketsController`: Main ticket management functionality
- `TicketAttachmentsController`: Attachment handling
- `TicketCommentsController`: Comment management
- `AuthController`: Authentication endpoints
- `DashboardController`: Dashboard statistics
- `AuditController`: Audit log management
- `NotificationsController`: Notification endpoints

## Services Implemented

- `SlaCalculator`: SLA calculation service
- `TicketAssignmentService`: Ticket assignment service
- `TicketLiveNotifier`: Real-time notification service
- `OutboxWriter`: Message outbox service
- `SlaMonitorService`: SLA breach monitoring service

## Database

The application uses Entity Framework Core with SQL Server. The database schema includes:

- Tickets table with all necessary fields
- TicketAttachments table for file attachments
- TicketComments table for comments
- OutboxMessages table for background job processing
- Audit logs for tracking changes

## Background Jobs

- Notification jobs for sending emails/webhooks
- SLA monitoring jobs
- Daily summary reports

## Authentication & Authorization

- JWT-based authentication
- Role-based authorization (Admin, Agent)
- Claims-based access control

## Testing

Unit tests are included for core functionality including:
- Ticket controller tests
- Application startup verification

## Getting Started

1. Update connection strings in `appsettings.json`
2. Run database migrations
3. Start the application

## Notes

This is a minimal implementation focused on core ticket management functionality. The implementation includes all the required interfaces and services but with simplified logic for demonstration purposes. In a production environment, you would want to implement more robust logic for SLA calculations, ticket assignment, and notification systems.