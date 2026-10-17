# Rule 28: An Outline only for the same steps

> Use a Scenario Outline only when every row runs the same steps and only the values change.

[All rules](../SKILL.md)

## Why

An Outline runs the same steps once for each row of its Examples table. It works well when the
rows vary business values within one behavior. If a row needs a different action or assertion,
write a separate scenario so that difference stays visible.

For example, adding an available product succeeds; adding an unavailable product is rejected.
Putting `sees` and `does not see` into a result column hides different outcomes inside step text.
Keep those outcomes explicit. A quantity or price placeholder is a value; a placeholder that
changes what the step does is a warning sign.

## Bad

```gherkin
Feature: Cart management

  Rule: Only products in stock can be added to a cart
    Scenario Outline: A customer adds a product
      Given a customer "C1" has a cart
      And a product "P1" is <stock state>
      When "C1" adds product "P1" to the cart
      Then "C1" <result> product "P1" in the cart

      Examples: Stock availability
        | stock state  | result       |
        | in stock     | sees         |
        | out of stock | does not see |
```

The result column changes the assertion's meaning instead of supplying an expected business
value.

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

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
