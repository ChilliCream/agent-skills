# Rule 3: A different starting state is a different scenario

> If the same act starts from a different state, write a separate scenario.

[All rules](../SKILL.md)

## Why

The same act can start from different states. For example, a customer can try to add a product
that is in stock or one that is out of stock. Give each state its own scenario so a failure shows
which case broke. A Scenario Outline can also express these cases when they share the same steps
(rule 28).

## Bad

```gherkin
Feature: Cart management

  Rule: Customers can add only products that are in stock
    Scenario: A customer adds products with different stock levels
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      And a product "P2" is out of stock
      When "C1" adds products "P1" and "P2" to the cart
      Then "C1" sees product "P1" in the cart
      And "C1" sees that product "P2" is out of stock
```

One scenario combines two starting states. If the check of `P1` fails, the run never checks `P2`.

## Good

```gherkin
Feature: Cart management

  Rule: Customers can add only products that are in stock
    Scenario: A customer adds a product that is in stock
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart

    Scenario: A customer tries to add a product that is out of stock
      Given a customer "C1" has an empty cart
      And a product "P1" is out of stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees that product "P1" is out of stock
      And "C1" sees an empty cart
```

Each scenario starts from one state, and its title names that state.
