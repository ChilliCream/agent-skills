# Rule 27: A Background holds only shared Givens

> A `Background` holds only the `Given` steps that every scenario in its scope needs.

[All rules](../SKILL.md)

## Why

A `Background` runs before each scenario in its Feature or Rule. Keep it focused on the shared
starting state. An action in the Background hides part of the behavior being tested; a condition
used by only one scenario distracts readers of the others.

For example, both cart scenarios need a customer with a cart. Stock availability differs between
the scenarios, so each scenario states its own stock condition and keeps adding the product as
its action.

Keep a Background short. If it grows beyond about four lines, look for a business-level step that
can express several related setup details together.

## Bad

```gherkin
Feature: Checkout

  Rule: Successful payment creates a confirmed order
    Background:
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart

    Scenario: A customer pays for the cart
      When "C1" pays for the cart successfully
      Then "C1" sees a confirmed order "O1"
```

The Background contains an action, so the scenario has two actions split across two locations.
For a checkout example, express the prepared cart as a starting condition.

## Good

```gherkin
Feature: Cart management

  Rule: Only products in stock can be added to a cart
    Background:
      Given a customer "C1" has a cart

    Scenario: A customer adds an available product
      Given a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart

    Scenario: A customer cannot add an unavailable product
      Given a product "P1" is out of stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees that product "P1" is out of stock
      And "C1" does not see product "P1" in the cart
```

The Background contains the common starting state; each scenario supplies its relevant variation
and its action.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
