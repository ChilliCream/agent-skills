# Rule 28: An Outline only for the same steps

> Use a Scenario Outline only when every row runs the same steps and only the values change.

[All rules](../SKILL.md)

## Why

An Outline runs the same steps once for each row of its table. That works when the rows differ only
in their values. If one row needs a different step, the shared steps hide the difference.

For example, a subscription that receives an item and one that receives nothing need different
`Then` steps: "sees" and "does not see". Write them as two scenarios. A table column that switches
between them only hides that they are two cases.

## Bad

```gherkin
Feature: Matching

  Rule: A subscription matches the items published to its target market
    Scenario Outline: A subscription receives the items of its target market
      Given a data source company "DS1" with an admin "U-DS"
      And a data recipient company "DR1" with an admin "U-DR"
      And "U-DS" has published a complete item "I1" for target market 208
      When "U-DR" subscribes to data source "DS1" for target market <market>
      Then "U-DR" <result> item "I1" from "DS1" on the Received Items page

      Examples: Target market of the subscription
        | market | result       |
        | 208    | sees         |
        | 752    | does not see |
```

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

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
