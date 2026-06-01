using AnnouncementBot.Application.Common.Interfaces;
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using MediatR;

namespace AnnouncementBot.Application.Commands.AdminRequests;

public record HandleAdminRequestCommand(
    Guid RequestId,
    long SuperAdminId,
    bool IsApproved,
    string? CategoryName = null
) : IRequest, IAuditableRequest
{
    public long ActorId => SuperAdminId;
    public string ActionName => IsApproved ? "Одобрение заявки в админы" : "Отклонение заявки в админы";
    public string EntityName => "AdminRequest";
    public string GetEntityId() => RequestId.ToString();
    public string? Details => IsApproved
        ? $"Заявка одобрена. Назначена категория: {CategoryName ?? "Не указана"}"
        : "Заявка отклонена супер-админом.";
}

public class HandleAdminRequestCommandHandler : IRequestHandler<HandleAdminRequestCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public HandleAdminRequestCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(HandleAdminRequestCommand request, CancellationToken ct)
    {
        var adminRequest = await _unitOfWork.AdminRequests.GetByIdAsync(request.RequestId, ct);
        if (adminRequest is null)
            throw new KeyNotFoundException($"Заявка с ID {request.RequestId} не найдена.");

        if (adminRequest.Status != AdminRequestStatus.Pending)
            throw new InvalidOperationException("Эта заявка уже была обработана.");

        if (request.IsApproved)
        {
            if (adminRequest.Type == AdminRequestType.Assignment)
            {
                if (string.IsNullOrWhiteSpace(request.CategoryName))
                    throw new ArgumentException("Необходимо указать название категории.");

                var category = await _unitOfWork.Categories.GetByNameAsync(request.CategoryName, ct);
                if (category is null)
                {
                    category = new Category(request.CategoryName, request.SuperAdminId);
                    await _unitOfWork.Categories.AddAsync(category, ct);
                }

                var accessExists = await _unitOfWork.AdminCategoryAccesses.ExistsAsync(adminRequest.RequesterId, category.Id, ct);
                if (!accessExists)
                {
                    var access = new AdminCategoryAccess(adminRequest.RequesterId, category.Id);
                    await _unitOfWork.AdminCategoryAccesses.AddAsync(access, ct);
                }

                var user = await _unitOfWork.Users.GetByIdAsync(adminRequest.RequesterId, ct);
                if (user is not null && user.Role == UserRole.User)
                {
                    user.ChangeRole(UserRole.Admin);
                    await _unitOfWork.Users.UpdateAsync(user, ct);
                }
            }
            else if (adminRequest.Type == AdminRequestType.Reassignment)
            {
                if (adminRequest.TargetId is null)
                    throw new InvalidOperationException("В заявке отсутствует целевой пользователь TargetId.");

                var oldAccesses = await _unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(adminRequest.RequesterId, ct);

                foreach (var access in oldAccesses)
                {
                    await _unitOfWork.AdminCategoryAccesses.DeleteAsync(access, ct);

                    var targetHasAccess = await _unitOfWork.AdminCategoryAccesses.ExistsAsync(adminRequest.TargetId.Value, access.CategoryId, ct);
                    if (!targetHasAccess)
                    {
                        var newAccess = new AdminCategoryAccess(adminRequest.TargetId.Value, access.CategoryId);
                        await _unitOfWork.AdminCategoryAccesses.AddAsync(newAccess, ct);
                    }
                }

                var targetUser = await _unitOfWork.Users.GetByIdAsync(adminRequest.TargetId.Value, ct);
                if (targetUser is not null && targetUser.Role == UserRole.User)
                {
                    targetUser.ChangeRole(UserRole.Admin);
                    await _unitOfWork.Users.UpdateAsync(targetUser, ct);
                }

                var requester = await _unitOfWork.Users.GetByIdAsync(adminRequest.RequesterId, ct);
                if (requester is not null && requester.Role == UserRole.Admin)
                {
                    requester.ChangeRole(UserRole.User);
                    await _unitOfWork.Users.UpdateAsync(requester, ct);
                }
            }

            adminRequest.Approve(request.SuperAdminId);
        }
        else
        {
            adminRequest.Reject(request.SuperAdminId);
        }

        await _unitOfWork.AdminRequests.UpdateAsync(adminRequest, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}