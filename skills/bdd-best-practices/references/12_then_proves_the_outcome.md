# Rule 12: A Then proves the outcome itself

> A `Then` checks the outcome itself, not a sign of it.

[All rules](../SKILL.md)

## Why

If a `Then` checks only a sign of the outcome, it can pass even though the act failed. The scenario
stays green while the behaviour is broken.

For example, after adding a product, a webshop shows the message "Added to cart". The message can
appear even if the cart was not updated. Check that the product is in the customer's cart. The
message can be an extra assertion, but it cannot be the only proof of the cart change.

The same goes for an "or". `"C1" sees product "P1" in the cart or an empty cart` passes even if
adding the product did nothing. Decide which outcome is correct, and check that outcome.

## Bad

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees the cart page
      And "C1" is told that product "P1" was added to the cart
```

A page and a confirmation message do not prove that the cart contains the product.

## Good

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" is told that product "P1" was added to the cart
      And "C1" sees product "P1" in the cart
```

The last step proves the cart change. Keep the confirmation assertion only when that message is
also part of the behaviour being specified.

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
