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
    Given a customer has an empty cart
    And product "Travel mug" is in stock
    When the customer adds "Travel mug" to the cart
    Then the customer sees "Travel mug" in the cart

  Scenario: A customer cannot add an unavailable product
    Given a customer has an empty cart
    And product "Travel mug" is out of stock
    When the customer adds "Travel mug" to the cart
    Then the customer sees that "Travel mug" is out of stock
    And the customer does not see "Travel mug" in the cart
```

The scenarios are valid, but the shared stock rule is left for readers to infer.

## Good

```gherkin
Feature: Cart management

  Rule: Only products in stock can be added to a cart
    Scenario: A customer adds an available product
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart

    Scenario: A customer cannot add an unavailable product
      Given a customer has an empty cart
      And product "Travel mug" is out of stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees that "Travel mug" is out of stock
      And the customer does not see "Travel mug" in the cart
```

The heading states the business rule that both examples illustrate.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
