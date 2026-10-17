# Rule 9: A Given says what is there

> A `Given` says what is already there, not how it got there.

[All rules](../SKILL.md)

## Why

A `Given` describes how the world looks when the scenario starts. It does not tell the story of how
it got that way.

For example, a cart scenario needs an in-stock product. Write `product "Travel mug" is in stock`. Avoid
recounting how a shop manager created the product, set its price and recorded its stock. Those
steps lengthen the scenario and couple it to product setup, even though cart behaviour has not
changed.

## Bad

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer has an empty cart
      And a shop manager creates product "Travel mug"
      And the shop manager sets a price for "Travel mug"
      And the shop manager records stock for "Travel mug"
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

Three steps tell how the product was prepared. The scenario only needs the resulting starting
state.

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

One step states the relevant product fact. Its setup can create a product available for purchase
without making those preparation details part of the business example.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
