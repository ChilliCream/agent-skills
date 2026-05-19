using Reference.Ddd.Ordering.Orders;

namespace Reference.Ddd.GraphQL.Ordering.Types;

[ObjectType<ShippingAddress>]
public static partial class ShippingAddressType
{
    static partial void Configure(IObjectTypeDescriptor<ShippingAddress> descriptor)
    {
        descriptor.BindFieldsImplicitly();
    }
}
