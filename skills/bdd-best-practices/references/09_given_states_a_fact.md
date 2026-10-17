# Rule 9: A Given says what is there

> A `Given` says what is already there, not how it got there.

[All rules](../SKILL.md)

## Why

A `Given` describes how the world looks when the scenario starts. It does not tell the story of how
it got that way.

For example, a cart scenario needs an in-stock product. Write `a product "P1" is in stock`. Avoid
recounting how a shop manager created the product, set its price and recorded its stock. Those
steps lengthen the scenario and couple it to product setup, even though cart behaviour has not
changed.

## Bad

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer "C1" has a cart
      And a shop manager "M1" creates product "P1"
      And "M1" sets a price for product "P1"
      And "M1" records stock for product "P1"
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

Three steps tell how the product was prepared. The scenario only needs the resulting starting
state.

## Good

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

One step states the relevant product fact. Its setup can create a product available for purchase
without making those preparation details part of the business example.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
