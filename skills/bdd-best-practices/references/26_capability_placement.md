# Rule 26: Place a scenario by its capability

> Put a scenario in the feature of the capability it is about, not of the screen it uses.

[All rules](../SKILL.md)

## Why

Screens move and change names, but business capabilities provide a more stable way to find
examples. A reader looks for payment behavior under checkout, even when the payment button also
appears on a cart page.

Choose the capability whose business rule the scenario demonstrates. A scenario about removing a
product belongs with cart management; a scenario about declining a payment belongs with checkout.
Follow the repository's directory conventions while keeping that distinction visible.

## Bad

```text
specs/cart-page/payment-declined.feature
specs/customer/remove-product.feature
specs/case-123/payment-declined.feature
```

These paths organize behavior by a screen, an actor, or a test-case identifier. None identifies
the capability whose rules the examples describe.

## Good

```text
specs/checkout/payment.feature
specs/cart/management.feature
```

The first feature holds payment outcomes, including a declined payment. The second holds cart
changes, including removing a product. Readers can find the behavior without knowing its screen
or the identifier of an earlier test case.
