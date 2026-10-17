# Rule 14: Keep it brief

> Aim for about five steps. If a scenario has more, look for a second behaviour or a detail that the
> rule does not need. _(smell)_

[All rules](../SKILL.md)

## Why

A short scenario is easy to read. If yours has more than about five steps, it often tests a second
behaviour or carries a detail that changes nothing. Move the second behaviour to its own scenario
and remove the detail.

For example, to show that a subscribed recipient receives a published item, you need a supplier, a
recipient and the subscription. A second data source or a draft item changes nothing, so leave it
out.

Five is not a hard limit. Every company and every user comes from a `Given`, so six or seven steps
can be correct. If the outcome needs every step, keep them all.

## Bad

```gherkin
Feature: Synchronisation

  Rule: A subscribed data recipient receives the items its data source publishes
    Scenario: A subscribed data recipient receives a newly published item
      Given a data source company "DS1" with an admin "U-DS"
      And a data source company "DS2" with an admin "U-DS2"
      And a data recipient company "DR1" with an admin "U-DR"
      And a data recipient company "DR2" with an admin "U-DR2"
      And "U-DR" is subscribed to data source "DS1"
      And "U-DR2" is subscribed to data source "DS2"
      And "U-DS" has a draft item "I2"
      When "U-DS" publishes a complete item "I1"
      Then "U-DS" is told that the item was published
      And "U-DR" sees item "I1" from "DS1" on the Received Items page
      And "U-DR2" does not see item "I1" from "DS1" on the Received Items page
```

`DS2` and the draft `I2` change nothing. The message to `U-DS` and the check of `U-DR2` belong to
other rules, so they go in other scenarios.

## Good

```gherkin
Feature: Synchronisation

  Rule: A subscribed data recipient receives the items its data source publishes
    Scenario: A subscribed data recipient receives a newly published item
      Given a data source company "DS1" with an admin "U-DS"
      And a data recipient company "DR1" with an admin "U-DR"
      And "U-DR" is subscribed to data source "DS1"
      When "U-DS" publishes a complete item "I1"
      Then "U-DR" sees item "I1" from "DS1" on the Received Items page
```

Each of the five steps is needed to show that `U-DR` receives `I1`.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
