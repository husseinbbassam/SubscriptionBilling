using FluentValidation;
using MediatR;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Abstractions;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Application.Customers;

public sealed record CreateCustomerCommand(string Name, string Email, string IdempotencyKey)
    : IIdempotentCommand<CreateCustomerResponse>;

public sealed record CreateCustomerResponse(Guid CustomerId);

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}

public sealed class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CreateCustomerResponse>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;

    public CreateCustomerHandler(ICustomerRepository customers, IUnitOfWork uow)
    {
        _customers = customers;
        _uow = uow;
    }

    public async Task<CreateCustomerResponse> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var customer = Customer.Create(request.Name, Email.Of(request.Email));
        await _customers.AddAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);
        return new CreateCustomerResponse(customer.Id.Value);
    }
}
