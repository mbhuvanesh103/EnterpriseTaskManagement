using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities;
using EnterpriseTaskManagement.Domain.Exceptions;
using EnterpriseTaskManagement.Domain.ValueObjects;
using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UpdateUserResponse> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Retrieve the existing user
        var user = await _userRepository.GetByPublicIdAsync(
            request.PublicId,
            cancellationToken);

        // Check if the user exists
        if (user is null)
        {
            throw new NotFoundException(
                nameof(User),
                request.PublicId);
        }

        // Create Value Objects
        var name = FullName.Create(
            request.FirstName,
            request.LastName);

        var email = Email.Create(request.Email);

        // Update the User Aggregate
        user.Update(
            name,
            email,
            request.DepartmentId,
            request.IsActive);

        // Persist changes
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Return response
        return new UpdateUserResponse(
            user.PublicId,
            "User updated successfully.");
    }
}