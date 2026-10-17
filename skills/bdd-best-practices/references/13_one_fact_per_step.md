# Rule 13: One fact per step

> Put one fact in each step.

[All rules](../SKILL.md)

## Why

When a step checks two things and fails, the report cannot tell you which one was wrong. That is
why each step states one fact.

For example, after a data recipient admin subscribes, you want to check the toast and the new
subscription. If one step checks both and it fails, you do not know whether the toast or the
subscription is missing. With two `Then` steps, the report names the one that failed.

Sometimes "and" is part of one fact. `a data source company "DS1" with an admin "U-DS"` creates one
thing, a company with its admin, so it stays one step.

## Bad

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" is told that the subscription was created and sees it on the Subscriptions page
```

## Good

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" is told that the subscription was created
      And "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

## Sources

- [Cucumber, Anti-patterns](https://cucumber.io/docs/guides/anti-patterns/)
