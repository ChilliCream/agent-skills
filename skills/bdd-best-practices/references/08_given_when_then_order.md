# Rule 8: Given, When, Then in that order

> First the `Given` steps, then one `When`, then the `Then` steps.

[All rules](../SKILL.md)

## Why

A scenario reads like a short story. The `Given` steps say what is already there, the `When` says
what someone does, and the `Then` steps say what happens as a result. Each part comes once, in this
order.

If you want to check something before the `When`, it belongs in a `Given`. For example, "the cart
is empty" is how the scenario starts, not a result of adding a product.

## Bad

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      Then "C1" sees an empty cart
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

The first `Then` checks how the scenario starts, before the action under test.

## Good

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer "C1" has a cart
      And the cart is empty
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

The empty cart is a starting fact. The final `Then` checks the result of adding the product.

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
