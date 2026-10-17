# Rule 25: Make business rules explicit with Rule

> Group scenarios under a `Rule` when the runner supports it and the heading clarifies the
> business rule illustrated by those scenarios.

[All rules](../SKILL.md)

## Why

A Feature describes a capability, such as cart management. A Rule names a business rule of that
capability, and its scenarios give examples. This grouping helps readers see why the examples
belong together.

`Rule` is optional in Gherkin. Use it when it improves the explanation and the project's runner
supports it. A short feature with one obvious rule can remain readable without this grouping.

## Bad

```gherkin
Feature: Cart management

  Scenario: A customer adds an available product
    Given a customer "C1" has a cart
    And a product "P1" is in stock
    When "C1" adds product "P1" to the cart
    Then "C1" sees product "P1" in the cart

  Scenario: A customer cannot add an unavailable product
    Given a customer "C1" has a cart
    And a product "P1" is out of stock
    When "C1" adds product "P1" to the cart
    Then "C1" sees that product "P1" is out of stock
    And "C1" does not see product "P1" in the cart
```

The scenarios are valid, but the shared stock rule is left for readers to infer.

## Good

```gherkin
Feature: Cart management

  Rule: Only products in stock can be added to a cart
    Scenario: A customer adds an available product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart

    Scenario: A customer cannot add an unavailable product
      Given a customer "C1" has a cart
      And a product "P1" is out of stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees that product "P1" is out of stock
      And "C1" does not see product "P1" in the cart
```

The heading states the business rule that both examples illustrate.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
