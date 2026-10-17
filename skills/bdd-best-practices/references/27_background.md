# Rule 27: A Background holds only shared Givens

> A `Background` holds only the `Given` steps that every scenario below it needs.

[All rules](../SKILL.md)

## Why

A `Background` runs before each scenario below it. A step that only one scenario needs makes all
the others harder to read. An act in a `Background` is worse, because every scenario then has two
acts.

For example, two scenarios check which subscription receives a published item. Both need the two
companies and the item, so these go in the `Background`. The subscription is the act of each
scenario, so it stays in its `When`.

Keep a `Background` short. If it grows beyond about four lines, look for one step that can replace
several.

## Bad

```gherkin
Feature: Matching

  Rule: A subscription matches the items published to its target market
    Background:
      Given a data source company "DS1" with an admin "U-DS"
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DS" publishes a complete item "I1" for target market 208

    Scenario: A subscription for the same target market receives the item
      When "U-DR" subscribes to data source "DS1" for target market 208
      Then "U-DR" sees item "I1" from "DS1" on the Received Items page
```

The publication is an act, so every scenario now has two acts.

## Good

```gherkin
Feature: Matching

  Rule: A subscription matches the items published to its target market
    Background:
      Given a data source company "DS1" with an admin "U-DS"
      And a data recipient company "DR1" with an admin "U-DR"
      And "U-DS" has published a complete item "I1" for target market 208

    Scenario: A subscription for the same target market receives the item
      When "U-DR" subscribes to data source "DS1" for target market 208
      Then "U-DR" sees item "I1" from "DS1" on the Received Items page

    Scenario: A subscription for another target market receives nothing
      When "U-DR" subscribes to data source "DS1" for target market 752
      Then "U-DR" does not see item "I1" from "DS1" on the Received Items page
```

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
