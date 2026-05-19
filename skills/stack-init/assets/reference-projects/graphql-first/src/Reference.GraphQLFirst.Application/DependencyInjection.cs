using Microsoft.Extensions.DependencyInjection;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Authors.Commands;
using Reference.GraphQLFirst.Application.Authors.Queries;
using Reference.GraphQLFirst.Application.Books.Commands;
using Reference.GraphQLFirst.Application.Books.Queries;

namespace Reference.GraphQLFirst.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReferenceGraphQLFirstApplication(
        this IServiceCollection services)
    {
        services
            .AddMediator()
            .AddHandler<CreateAuthorCommandHandler>()
            .AddHandler<GetAuthorByIdQueryHandler>()
            .AddHandler<CreateBookCommandHandler>()
            .AddHandler<GetBookByIdQueryHandler>()
            .AddHandler<PageBooksByAuthorIdQueryHandler>();

        return services;
    }
}
