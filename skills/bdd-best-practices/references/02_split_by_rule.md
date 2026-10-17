# Rule 2: Split consequences by rule

> If one act has consequences under different rules, write one scenario for each rule.

[All rules](../SKILL.md)

## Why

A scenario that checks the consequences of two rules breaks when either rule changes. It is then
hard to tell which rule the failure is about.

For example, when a data recipient admin subscribes to a data source, two things happen. The
subscription shows on the Subscriptions page, which is a rule about subscriptions. The subscription
also starts the delivery workflows, which is a rule about delivery. So you write two scenarios with
the same `When`, one in each feature.

## Bad

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
      And the subscription of "DR1" to "DS1" is Done on the Workflows page
```

The last step checks a delivery rule inside a scenario about subscriptions.

## Good

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

```gherkin
Feature: Synchronisation

  Rule: A subscription to a data source starts the delivery workflows
    Scenario: A new subscription starts the delivery workflows
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then the subscription of "DR1" to "DS1" is Done on the Workflows page
```

The first scenario goes in `subscriptions/management.feature`, the second in
`delivery/synchronisation.feature`.

## Sources

- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
