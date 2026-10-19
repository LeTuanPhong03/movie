using FluentValidation;
using MediatR;

namespace MovieReservation.Application.Common.Behaviors;
/// <summary>
/// ValidationBehavior là một lớp xử lý pipeline của MediatR, được sử dụng để thực hiện việc validate các request trước khi chúng được xử lý.
/// Nó sử dụng FluentValidation để kiểm tra các request dựa trên các validator đã được đăng ký trong DI container. Nếu có bất kỳ lỗi validate nào, nó sẽ ném ra một ValidationException.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();
            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }
        return await next();
    }
}