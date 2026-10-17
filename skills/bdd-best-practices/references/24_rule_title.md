# Rule 24: A Rule title is a rule

> Write a Rule title as a sentence that can be true or false.

[All rules](../SKILL.md)

## Why

The Rule line tells the reader which business rule the scenarios below it show. A topic such as
"Subscriptions" or a test case number does not tell the reader what GDSN must do.

For example, `A subscription targets one data source in one target market` is a rule. You can check
whether GDSN follows it.

## Bad

```gherkin
Feature: Subscription management

  Rule: Subscriptions
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

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

## Sources

- sync-autotest specification guide, _The `specs/` tree_
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
