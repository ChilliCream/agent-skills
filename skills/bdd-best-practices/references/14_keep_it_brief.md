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
      Given a customer "C1" has a cart
      And a customer "C2" has a cart
      And a product "P1" is in stock
      And a product "P2" is in stock
      And "C2" has product "P2" in the cart
      And "C1" has a saved delivery address
      When "C1" adds product "P1" to the cart
      Then "C1" sees a confirmation that product "P1" was added to the cart
      And "C1" sees product "P1" in the cart
      And "C2" does not see product "P1" in the cart
```

`P2` and the delivery address change nothing. The confirmation and the isolation of `C2`'s cart
belong to other rules, so they go in other scenarios.

## Good

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: A customer adds an in-stock product to the cart
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

Each of the four steps is needed to show that `C1` can add `P1` to the cart.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
