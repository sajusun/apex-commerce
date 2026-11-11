using MediatR;
using ApexCommerce.Domain.Exceptions;
using ApexCommerce.Domain.Repositories;

namespace ApexCommerce.Application.Features.Admin.Customers.Commands.UpdateCustomerStatus;

public sealed record UpdateCustomerStatusCommand(Guid CustomerId, bool IsActive) : IRequest;

public sealed class UpdateCustomerStatusCommandHandler : IRequestHandler<UpdateCustomerStatusCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerStatusCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateCustomerStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new EntityNotFoundException("User", request.CustomerId);

        user.SetStatus(request.IsActive);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

