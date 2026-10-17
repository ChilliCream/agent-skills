# Rule 13: One fact per step

> Put one fact in each step.

[All rules](../SKILL.md)

## Why

When a step checks two things and fails, the report cannot tell you which one was wrong. That is
why each step states one fact.

For example, after a customer adds a product to a cart, a scenario may check both the confirmation
and the cart contents. If one step checks both and fails, you do not know which outcome is wrong.
With two `Then` steps, the report names the failed expectation.

One fact can include a relationship. `a customer "C1" has a cart` establishes one starting fact;
it does not need separate steps for the customer and the cart.

## Bad

```gherkin
Feature: Shopping cart

  Rule: Adding an in-stock product confirms the addition and updates the cart
    Scenario: A customer adds an in-stock product to the cart
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees a confirmation and sees product "P1" in the cart
```

## Good

```gherkin
Feature: Shopping cart

  Rule: Adding an in-stock product confirms the addition and updates the cart
    Scenario: A customer adds an in-stock product to the cart
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees a confirmation that product "P1" was added to the cart
      And "C1" sees product "P1" in the cart
```

## Sources

- [Cucumber, Anti-patterns](https://cucumber.io/docs/guides/anti-patterns/)
