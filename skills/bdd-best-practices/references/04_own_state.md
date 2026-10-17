# Rule 4: Every scenario creates its own state

> Every scenario creates everything it needs. It never relies on another scenario.

[All rules](../SKILL.md)

## Why

Scenarios can run in parallel and in any order. A scenario that needs another scenario to run
first fails as soon as the order changes.

For example, one scenario adds "Travel mug" to a cart, and the next one removes it. The second
scenario only works if the first ran before it and left its state behind. Give the second scenario
its own `Given` steps so it has its own customer, cart and product.

The role "the customer" and product name "Travel mug" identify records within each scenario.
They make the story readable; they do not establish shared records across scenarios. Setup must
isolate the actual mutable records, including cart contents and stock, even when the same role or
product name appears in several scenarios.

## Bad

```gherkin
Feature: Cart management

  Rule: Customers can add products that are in stock
    Scenario: A customer adds a product
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart

  Rule: Customers can remove products from their cart
    Scenario: A customer removes the only product
      When the customer removes "Travel mug" from the cart
      Then the customer sees an empty cart
```

The second scenario has no `Given`. On its own, it has no customer or cart containing "Travel mug".

## Good

```gherkin
Feature: Cart management

  Rule: Customers can remove products from their cart
    Scenario: A customer removes the only product
      Given a customer has a cart
      And the customer's cart contains only "Travel mug"
      When the customer removes "Travel mug" from the cart
      Then the customer sees an empty cart
```

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
