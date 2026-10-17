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
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      And product "Coffee beans" is out of stock
      When the customer adds "Travel mug" and "Coffee beans" to the cart
      Then the customer sees "Travel mug" in the cart
      And the customer sees that "Coffee beans" is out of stock
```

One scenario combines two starting states. If the check for "Travel mug" fails, the run never
checks the result for "Coffee beans".

## Good

```gherkin
Feature: Cart management

  Rule: Customers can add only products that are in stock
    Scenario: A customer adds a product that is in stock
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart

    Scenario: A customer tries to add a product that is out of stock
      Given a customer has an empty cart
      And product "Travel mug" is out of stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees that "Travel mug" is out of stock
      And the customer sees an empty cart
```

Each scenario starts from one state, and its title names that state.
