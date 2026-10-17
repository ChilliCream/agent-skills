# Rule 8: Given, When, Then in that order

> First the `Given` steps, then one `When`, then the `Then` steps.

[All rules](../SKILL.md)

## Why

A scenario reads like a short story. The `Given` steps say what is already there, the `When` says
what someone does, and the `Then` steps say what happens as a result. Each part comes once, in this
order.

If you want to check something before the `When`, it belongs in a `Given`. For example, "the item
is still a draft" is not a result of anything. It is how the scenario starts.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has a complete item "I1"
      Then "U-DS" sees item "I1" on the Items page as Draft
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published
```

The first `Then` checks how the scenario starts, before anything has happened.

## Good

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has a complete item "I1"
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published
```

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
