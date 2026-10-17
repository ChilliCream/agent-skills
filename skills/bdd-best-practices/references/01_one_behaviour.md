# Rule 1: One behaviour per scenario

> Test one behaviour in each scenario. A second act belongs in a second scenario.

[All rules](../SKILL.md)

## Why

A scenario that tests several behaviours one after the other stops at the first failure. You then
do not know whether the rest still works.

For example, a scenario adds a product to a cart and then removes it. If adding breaks, the
scenario stops there, and nobody learns whether removing still works. Write one scenario for each
behaviour instead. The removal scenario starts with the product already in the cart (rule 5).

## Bad

```gherkin
Feature: Cart management

  Rule: Customers can add and remove products
    Scenario: A customer adds and removes a product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
      When "C1" removes product "P1" from the cart
      Then "C1" sees an empty cart
```

Two behaviours share one scenario. If adding fails, removal is never tested.

## Good

```gherkin
Feature: Cart management

  Rule: Customers can add products that are in stock
    Scenario: A customer adds a product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

```gherkin
Feature: Cart management

  Rule: Customers can remove products from their cart
    Scenario: A customer removes the only product
      Given a customer "C1" has a cart
      And the cart of "C1" contains only product "P1"
      When "C1" removes product "P1" from the cart
      Then "C1" sees an empty cart
```

Each behaviour has its own scenario, and each one starts from the state it needs.

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [Automation Panda, Are Gherkin scenarios with multiple When-Then pairs okay?](https://automationpanda.com/2018/02/03/are-gherkin-scenarios-with-multiple-when-then-pairs-okay/)
