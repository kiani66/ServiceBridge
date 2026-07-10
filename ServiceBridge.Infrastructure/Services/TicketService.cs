using ServiceBridge.Application.Audit;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class TicketService : ITicketService
{
    private const int TicketNumberAttempts = 5;

    private readonly ITicketRepository _repository;
    private readonly ITicketNumberGenerator _ticketNumberGenerator;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogWriter _auditLogWriter;

    public TicketService(
        ITicketRepository repository,
        ITicketNumberGenerator ticketNumberGenerator,
        INotificationService notificationService,
        IAuditLogWriter auditLogWriter)
    {
        _repository = repository;
        _ticketNumberGenerator = ticketNumberGenerator;
        _notificationService = notificationService;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<ServiceResult<PagedResultDto<TicketListItemDto>>>
        GetMyTicketsAsync(
            Guid userId,
            int page,
            int pageSize,
            TicketStatus? status)
    {
        page = Math.Max(page, 1);
        pageSize = NormalizePageSize(pageSize);

        return ServiceResult<PagedResultDto<TicketListItemDto>>.Ok(
            await _repository.GetMyTicketsAsync(userId, page, pageSize, status));
    }

    public async Task<ServiceResult<TicketDetailDto>> GetMyTicketAsync(
        Guid userId,
        Guid ticketId)
    {
        var ticket = await _repository.GetTicketDetailAsync(
            ticketId,
            userId,
            includeInternalMessages: false);

        return ticket == null
            ? TicketNotFound<TicketDetailDto>()
            : ServiceResult<TicketDetailDto>.Ok(ticket);
    }

    public async Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(
        Guid userId,
        CreateTicketRequest request)
    {
        var validation = await ValidateCreateAsync(userId, request);
        if (validation != null)
        {
            return validation;
        }

        var now = DateTime.UtcNow;
        var ticketNumber = await GenerateUniqueTicketNumberAsync(now);
        if (ticketNumber == null)
        {
            return ServiceResult<TicketDetailDto>.Fail(
                "Unable to generate a unique ticket number.",
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var ticket = Ticket.Create(
            userId,
            ticketNumber,
            request.Subject,
            request.Body,
            EmptyToNull(request.OrderId),
            categoryId: null,
            now);

        await _repository.AddTicketAsync(ticket);
        await _notificationService.QueueTicketCreatedForAdminsAsync(
            ticket.Id,
            ticket.TicketNumber);

        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult<TicketDetailDto>.Fail(
                "The ticket changed while it was being created. Please try again.",
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await _notificationService.PublishQueuedRealtimeAsync();

        var created = await _repository.GetTicketDetailAsync(
            ticket.Id,
            userId,
            includeInternalMessages: false);

        return ServiceResult<TicketDetailDto>.Created(
            created!,
            "Ticket created successfully.");
    }

    public async Task<ServiceResult> AddUserMessageAsync(
        Guid userId,
        Guid ticketId,
        CreateTicketMessageRequest request)
    {
        if (request.IsInternal)
        {
            return ValidationFailed(new Dictionary<string, string[]>
            {
                [nameof(request.IsInternal)] = ["Customers cannot create internal messages."]
            });
        }

        var bodyValidation = ValidateBody(request.Body);
        if (bodyValidation != null)
        {
            return bodyValidation;
        }

        var ticket = await _repository.GetTicketForUpdateAsync(ticketId, userId);
        if (ticket == null)
        {
            return TicketNotFound();
        }

        try
        {
            ticket.AddUserReply(userId, request.Body, DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            return ServiceResult.Fail(
                exception.Message,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await _notificationService.QueueTicketUserReplyForAdminsAsync(
            ticket.Id,
            ticket.TicketNumber);

        return await SaveOrConflictAsync("Ticket reply submitted successfully.");
    }

    public async Task<ServiceResult<PagedResultDto<TicketListItemDto>>>
        GetAdminTicketsAsync(
            int page,
            int pageSize,
            TicketStatus? status,
            Guid? userId,
            Guid? orderId,
            string? search)
    {
        page = Math.Max(page, 1);
        pageSize = NormalizePageSize(pageSize);

        return ServiceResult<PagedResultDto<TicketListItemDto>>.Ok(
            await _repository.GetAdminTicketsAsync(
                page,
                pageSize,
                status,
                userId,
                orderId,
                search));
    }

    public async Task<ServiceResult<TicketDetailDto>> GetAdminTicketAsync(
        Guid ticketId)
    {
        var ticket = await _repository.GetTicketDetailAsync(
            ticketId,
            userId: null,
            includeInternalMessages: true);

        return ticket == null
            ? TicketNotFound<TicketDetailDto>()
            : ServiceResult<TicketDetailDto>.Ok(ticket);
    }

    public async Task<ServiceResult> AddAdminMessageAsync(
        Guid adminId,
        Guid ticketId,
        CreateTicketMessageRequest request)
    {
        var bodyValidation = ValidateBody(request.Body);
        if (bodyValidation != null)
        {
            return bodyValidation;
        }

        var ticket = await _repository.GetTicketForUpdateAsync(ticketId, userId: null);
        if (ticket == null)
        {
            return TicketNotFound();
        }

        try
        {
            ticket.AddAdminMessage(
                adminId,
                request.Body,
                request.IsInternal,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            return ServiceResult.Fail(
                exception.Message,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        if (!request.IsInternal)
        {
            await _notificationService.QueueTicketPublicReplyForUserAsync(
                ticket.UserId,
                ticket.Id,
                ticket.TicketNumber);
        }

        return await SaveOrConflictAsync("Ticket message added successfully.");
    }

    public async Task<ServiceResult> CloseTicketAsync(Guid adminId, Guid ticketId)
    {
        var ticket = await _repository.GetTicketForUpdateAsync(ticketId, userId: null);
        if (ticket == null)
        {
            return TicketNotFound();
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return ServiceResult.Ok("Ticket closed successfully.");
        }

        var beforeStatus = ticket.Status;
        ticket.Close(adminId, DateTime.UtcNow);
        await _notificationService.QueueTicketClosedForUserAsync(
            ticket.UserId,
            ticket.Id,
            ticket.TicketNumber);

        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult.Fail(
                "The ticket changed while it was being updated. Please refresh and try again.",
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await _auditLogWriter.WriteAsync(new AuditLogEntry
        {
            ActorId = adminId,
            ActorRole = nameof(UserRole.Admin),
            ActionType = AuditActionType.TicketClosed,
            EntityName = nameof(Ticket),
            EntityId = ticket.Id,
            Before = new { Status = beforeStatus.ToString() },
            After = new { Status = TicketStatus.Closed.ToString() },
            Description = $"Support ticket {ticket.TicketNumber} was closed by admin."
        });
        await _notificationService.PublishQueuedRealtimeAsync();

        return ServiceResult.Ok("Ticket closed successfully.");
    }

    private async Task<ServiceResult<TicketDetailDto>?> ValidateCreateAsync(
        Guid userId,
        CreateTicketRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            errors[nameof(request.Subject)] = ["Subject is required."];
        }
        else if (request.Subject.Trim().Length > 200)
        {
            errors[nameof(request.Subject)] = ["Subject must be 200 characters or less."];
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            errors[nameof(request.Body)] = ["Message is required."];
        }
        else if (request.Body.Trim().Length > 4000)
        {
            errors[nameof(request.Body)] = ["Message must be 4000 characters or less."];
        }

        if (errors.Count > 0)
        {
            return ValidationFailed<TicketDetailDto>(errors);
        }

        if (!await _repository.UserExistsAsync(userId))
        {
            return ServiceResult<TicketDetailDto>.Fail(
                AppMessages.UserNotFound,
                AppConstants.ErrorCodes.UserNotFound,
                404);
        }

        var orderId = EmptyToNull(request.OrderId);
        if (orderId.HasValue &&
            !await _repository.OrderBelongsToUserAsync(userId, orderId.Value))
        {
            return ValidationFailed<TicketDetailDto>(new Dictionary<string, string[]>
            {
                [nameof(request.OrderId)] =
                    ["Tickets can only be linked to one of your own orders."]
            });
        }

        return null;
    }

    private async Task<string?> GenerateUniqueTicketNumberAsync(DateTime now)
    {
        for (var attempt = 0; attempt < TicketNumberAttempts; attempt++)
        {
            var candidate = _ticketNumberGenerator.Generate(now);
            if (!await _repository.TicketNumberExistsAsync(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static ServiceResult? ValidateBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return ValidationFailed(new Dictionary<string, string[]>
            {
                [nameof(CreateTicketMessageRequest.Body)] = ["Message is required."]
            });
        }

        return body.Trim().Length > 4000
            ? ValidationFailed(new Dictionary<string, string[]>
            {
                [nameof(CreateTicketMessageRequest.Body)] =
                    ["Message must be 4000 characters or less."]
            })
            : null;
    }

    private async Task<ServiceResult> SaveOrConflictAsync(string successMessage)
    {
        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult.Fail(
                "The ticket changed while it was being updated. Please refresh and try again.",
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await _notificationService.PublishQueuedRealtimeAsync();
        return ServiceResult.Ok(successMessage);
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0
            ? ITicketService.DefaultPageSize
            : Math.Min(pageSize, ITicketService.MaximumPageSize);
    }

    private static Guid? EmptyToNull(Guid? value)
    {
        return value.HasValue && value.Value == Guid.Empty ? null : value;
    }

    private static ServiceResult TicketNotFound()
    {
        return ServiceResult.Fail(
            "Ticket not found.",
            AppConstants.ErrorCodes.NotFound,
            404);
    }

    private static ServiceResult<T> TicketNotFound<T>()
    {
        return ServiceResult<T>.Fail(
            "Ticket not found.",
            AppConstants.ErrorCodes.NotFound,
            404);
    }

    private static ServiceResult ValidationFailed(
        Dictionary<string, string[]> errors)
    {
        return new ServiceResult
        {
            Success = false,
            Message = AppMessages.ValidationFailed,
            ErrorCode = AppConstants.ErrorCodes.ValidationFailed,
            StatusCode = 400,
            ValidationErrors = errors
        };
    }

    private static ServiceResult<T> ValidationFailed<T>(
        Dictionary<string, string[]> errors)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Message = AppMessages.ValidationFailed,
            ErrorCode = AppConstants.ErrorCodes.ValidationFailed,
            StatusCode = 400,
            ValidationErrors = errors
        };
    }
}
