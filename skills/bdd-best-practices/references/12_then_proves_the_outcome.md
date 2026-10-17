# Rule 12: A Then proves the outcome itself

> A `Then` checks the outcome itself, not a sign of it.

[All rules](../SKILL.md)

## Why

If a `Then` checks only a sign of the outcome, it can pass even though the act failed. The scenario
stays green while the behaviour is broken.

For example, after a subscription, GDSN shows the toast "Subscription created!". The toast can
appear even if no subscription was saved. So check the subscription itself on the Subscriptions
page. The toast can be an extra `Then`, but never the only one.

The same goes for an "or". `"U-DS" sees item "I1" as Published or Draft` passes even if publishing
did nothing. Decide which outcome is correct, and check only that one.

## Bad

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" is on the Subscriptions page
      And "U-DR" is told that the subscription was created
```

A page and a toast. Neither proves that the subscription exists.

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

The last step proves the subscription, so the toast is only extra.

## Sources

- sync-autotest specification guide, _Inputs and the behaviour outline_
- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
