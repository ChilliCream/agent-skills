# Rule 2: Split consequences by rule

> If one act has consequences under different rules, write one scenario for each rule.

[All rules](../SKILL.md)

## Why

A scenario that checks the consequences of two rules breaks when either rule changes. It is then
hard to tell which rule the failure is about.

For example, checkout confirms an order for the customer and reduces the available stock for the
shop manager. Order confirmation and stock availability are separate business rules. Write two
scenarios with the same `When`, each with the setup and outcome relevant to its rule.

## Bad

```gherkin
Feature: Checkout

  Rule: A successful checkout confirms the customer's order
    Scenario: A customer checks out
      Given a customer "C1" has a cart
      And the cart of "C1" contains one unit of product "P1"
      And one unit of product "P1" is available
      And a shop manager "M1" manages the stock
      When "C1" checks out
      Then "C1" sees order "O1" as confirmed
      And "M1" sees no available stock for product "P1"
```

The last step checks a stock rule inside a scenario about order confirmation.

## Good

```gherkin
Feature: Checkout

  Rule: A successful checkout confirms the customer's order
    Scenario: A customer checks out
      Given a customer "C1" has a cart
      And the cart of "C1" contains one unit of product "P1"
      And one unit of product "P1" is available
      When "C1" checks out
      Then "C1" sees order "O1" as confirmed
```

```gherkin
Feature: Stock management

  Rule: Checkout reduces available stock by the ordered quantity
    Scenario: A customer buys the last available unit
      Given a customer "C1" has a cart
      And the cart of "C1" contains one unit of product "P1"
      And one unit of product "P1" is available
      And a shop manager "M1" manages the stock
      When "C1" checks out
      Then "M1" sees no available stock for product "P1"
```

The first scenario belongs in the checkout feature; the second belongs in the stock management
feature. Each fails for the business rule it describes.

## Sources

- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
