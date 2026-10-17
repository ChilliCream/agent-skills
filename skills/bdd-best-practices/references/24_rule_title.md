# Rule 24: A Rule title is a rule

> Write a Rule title as a sentence that can be true or false.

[All rules](../SKILL.md)

## Why

The Rule line tells the reader which business rule the scenarios below it show. A topic such as
"Shopping carts" or a test case number does not tell the reader what the webshop must do.

For example, `A customer can add an in-stock product to the cart` is a rule. You can check whether
the webshop follows it.

## Bad

```gherkin
Feature: Shopping cart

  Rule: Shopping carts
    Scenario: A customer adds a product to the cart
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

## Good

```gherkin
Feature: Shopping cart

  Rule: A customer can add an in-stock product to the cart
    Scenario: A customer adds a product to the cart
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
