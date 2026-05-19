using Reference.Ddd.Catalog.Products;

namespace Reference.Ddd.GraphQL.Catalog.Types;

[ObjectType<Sku>]
public static partial class SkuType
{
    static partial void Configure(IObjectTypeDescriptor<Sku> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Value);
    }
}
