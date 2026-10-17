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
      Given a customer has a cart
      And product "Travel mug" is in stock
      Then the customer sees an empty cart
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

The first `Then` checks how the scenario starts, before the action under test.

## Good

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

The empty cart is a starting fact. The final `Then` checks the result of adding the product.

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
