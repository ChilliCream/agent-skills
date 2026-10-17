# Rule 29: Each Examples row is a new case

> Each row of an `Examples` table tests a different case. Give the table a title that says what
> changes.

[All rules](../SKILL.md)

## Why

A row that tests the same case as another row adds run time but no information. Each row should be
a case that can fail on its own.

For example, four rows where the item and the subscription have the same target market all test
the same case: the markets match. One example is enough.

The title of the table tells the reader why the rows differ, for example
`Examples: Limit that splits the message`.

## Bad

```gherkin
Feature: Matching

  Rule: A subscription matches the items published to its target market
    Scenario Outline: A subscription for the same target market receives the item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has published a complete item "I1" for target market <market>
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1" for target market <market>
      Then "U-DR" sees item "I1" from "DS1" on the Received Items page

      Examples:
        | market |
        | 208    |
        | 752    |
        | 528    |
        | 276    |
```

Four rows of the same case, and a table without a title.

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

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
