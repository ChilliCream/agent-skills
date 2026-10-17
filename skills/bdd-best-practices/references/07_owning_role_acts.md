# Rule 7: The role that normally does it acts

> Let the role that normally does the act do it. Write a scenario for another role only if that role
> has a user story of its own or gets a different result.

[All rules](../SKILL.md)

## Why

Often several roles can do the same thing. Write the scenario once, for the role that normally does
it.

For example, a customer normally cancels their own unpaid order. A shop manager may also be able to
cancel it, but repeating the same scenario for the manager adds no new business rule.

There are two exceptions. Each is a rule of its own, and it gets its own scenario:

- The role has a user story of its own: "As a shop manager, I want to cancel an order at a customer's
  request". The result can be the same as for the customer.
- The role gets a different result. For example, a customer cannot change a product's stock level,
  while a shop manager can.

## Bad

```gherkin
Feature: Order cancellation

  Rule: An unpaid order can be cancelled
    Scenario: A shop manager cancels an unpaid order
      Given a customer "C1" has an unpaid order "O1"
      And a shop manager "M1" can manage customer orders
      When "M1" cancels order "O1"
      Then "M1" sees order "O1" as Cancelled

    Scenario: A customer cancels an unpaid order
      Given a customer "C1" has an unpaid order "O1"
      When "C1" cancels order "O1"
      Then "C1" sees order "O1" as Cancelled
```

The same behaviour appears twice. The customer normally cancels their own order, and this rule
states no separate need for the manager.

## Good

```gherkin
Feature: Order cancellation

  Rule: An unpaid order can be cancelled
    Scenario: A customer cancels an unpaid order
      Given a customer "C1" has an unpaid order "O1"
      When "C1" cancels order "O1"
      Then "C1" sees order "O1" as Cancelled
```

```gherkin
Feature: Order cancellation

  Rule: A shop manager can cancel an unpaid order at the customer's request
    Scenario: A shop manager handles a cancellation request
      Given a customer "C1" has an unpaid order "O1"
      And a shop manager "M1" can manage customer orders
      And "C1" has requested cancellation of order "O1"
      When "M1" cancels order "O1"
      Then "M1" sees order "O1" as Cancelled
```

The manager has a user story of their own, so its Rule names that story.

```gherkin
Feature: Stock management

  Rule: Only shop managers can change a product's stock level
    Scenario: A customer tries to change a stock level
      Given a customer "C1" exists
      And product "P1" has a stock level of 3
      When "C1" attempts to change the stock level of product "P1" to 5
      Then "C1" is told that only shop managers can change stock levels
      And "C1" sees product "P1" with a stock level of 3
```

The customer gets a different result from the manager, so the customer gets a scenario of their own.
Use an actor's supported interface for this check; if customers cannot attempt the action, describe
that restriction through the customer-visible interface instead.
