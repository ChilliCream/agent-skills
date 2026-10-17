# Rule 14: Keep it brief

> Aim for about five steps. If a scenario has more, look for a second behaviour or a detail that the
> rule does not need. _(smell)_

[All rules](../SKILL.md)

## Why

A short scenario is easy to read. If yours has more than about five steps, it often tests a second
behaviour or carries a detail that changes nothing. Move the second behaviour to its own scenario
and remove the detail.

For example, to show that a customer can add an in-stock product to a cart, you need the customer,
the cart and the product's stock state. Another customer's cart and a delivery address change
nothing, so leave them out.

Five is not a hard limit. A scenario may need several actors or starting facts, so six or seven
steps can be correct. If the outcome needs every step, keep them all.

## Bad

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: A customer adds an in-stock product to the cart
      Given a customer named Alice has an empty cart
      And a customer named Bob has a cart
      And product "Travel mug" is in stock
      And product "Coffee beans" is in stock
      And Bob has "Coffee beans" in the cart
      And Alice has a saved delivery address
      When Alice adds "Travel mug" to the cart
      Then Alice sees a confirmation that "Travel mug" was added to the cart
      And Alice sees "Travel mug" in the cart
      And Bob does not see "Travel mug" in the cart
```

The coffee beans and the delivery address change nothing. The confirmation and the isolation of
Bob's cart belong to other rules, so they go in other scenarios.

## Good

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: A customer adds an in-stock product to the cart
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

Each of the four steps is needed to show that the customer can add the travel mug to the cart.
With only one customer left, the role identifies the actor without a name.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
