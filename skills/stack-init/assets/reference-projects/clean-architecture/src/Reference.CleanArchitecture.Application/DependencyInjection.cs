using Microsoft.Extensions.DependencyInjection;
using Mocha.Mediator;
using Reference.CleanArchitecture.Application.Authors.Commands;
using Reference.CleanArchitecture.Application.Authors.Queries;
using Reference.CleanArchitecture.Application.Books.Commands;
using Reference.CleanArchitecture.Application.Books.Queries;

namespace Reference.CleanArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReferenceCleanArchitectureApplication(
        this IServiceCollection services)
    {
        services
            .AddMediator()
            .AddHandler<RegisterAuthorCommandHandler>()
            .AddHandler<GetAuthorByIdQueryHandler>()
            .AddHandler<AddBookToAuthorCommandHandler>()
            .AddHandler<PublishBookCommandHandler>()
            .AddHandler<GetBookByIdQueryHandler>()
            .AddHandler<GetBooksByAuthorIdQueryHandler>();

        return services;
    }
}
