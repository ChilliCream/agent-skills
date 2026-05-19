using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.GraphQL.Shared;

[ObjectType<Money>]
public static partial class MoneyType
{
    static partial void Configure(IObjectTypeDescriptor<Money> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Amount);
        descriptor.Field(x => x.Currency);
    }
}
