# Rule 4: Every scenario creates its own state

> Every scenario creates everything it needs. It never relies on another scenario.

[All rules](../SKILL.md)

## Why

Scenarios can run in parallel and in any order. A scenario that needs another scenario to run
first fails as soon as the order changes.

For example, one scenario adds product "P1" to a cart, and the next one removes it. The second
scenario only works if the first ran before it and left its state behind. Give the second scenario
its own `Given` steps so it has its own customer, cart and product. Aliases can repeat across
scenarios because each scenario resolves them within its own state.

## Bad

```gherkin
Feature: Cart management

  Rule: Customers can add products that are in stock
    Scenario: A customer adds a product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart

  Rule: Customers can remove products from their cart
    Scenario: A customer removes the only product
      When "C1" removes product "P1" from the cart
      Then "C1" sees an empty cart
```

The second scenario has no `Given`. On its own, it has no customer "C1" or product "P1" in a cart.

## Good

```gherkin
Feature: Cart management

  Rule: Customers can remove products from their cart
    Scenario: A customer removes the only product
      Given a customer "C1" has a cart
      And the cart of "C1" contains only product "P1"
      When "C1" removes product "P1" from the cart
      Then "C1" sees an empty cart
```

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
