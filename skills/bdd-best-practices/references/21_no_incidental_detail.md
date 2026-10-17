# Rule 21: No detail the rule does not need

> Leave out every value and every step that the rule does not need.

[All rules](../SKILL.md)

## Why

Every extra detail makes the reader look for a reason that is not there, and it is one more thing
that can break.

For example, a scenario about target markets needs target market 208, so it stays. It does not need
the publication date or the number of users, so they go.

If a date matters, write it relative to the run, such as `10 days from today`. A fixed date is
wrong the next day.

## Bad

```gherkin
Feature: Matching

  Rule: A subscription matches the items published to its target market
    Scenario: A subscription for the same target market receives the item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has published a complete item "I1" for target market 208 on 2026-10-05
      And a data recipient company "DR1" with an admin "U-DR" and 3 users
      When "U-DR" subscribes to data source "DS1" for target market 208
      Then "U-DR" sees item "I1" from "DS1" on the Received Items page
```

The date and the three users change nothing about matching.

## Good

```gherkin
Feature: Matching

  Rule: A subscription matches the items published to its target market
    Scenario: A subscription for the same target market receives the item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has published a complete item "I1" for target market 208
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1" for target market 208
      Then "U-DR" sees item "I1" from "DS1" on the Received Items page
```

## Sources

- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
