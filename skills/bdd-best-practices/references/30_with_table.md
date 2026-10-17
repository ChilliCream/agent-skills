# Rule 30: Avoid tables

> Do not use a table in a `Given` or a `When`. Write values in the step sentence and a list as
> separate lines. Use a table only for a `Then` whose rows are one outcome.

[All rules](../SKILL.md)

## Why

A scenario is easiest to read when every step is a plain sentence. With a table, the reviewer has
to look up each value's column heading to know what it means.

Write the values of one step in its sentence, for example `"U-DR" subscribes to data source "DS1"
for target market 208`. An optional value does not need a table either: the step with the value and
the step without it are two sentences. If a sentence gets too long, the step probably carries a
value that the rule does not need (rule 21).

For a list, write one line per entry. If a scenario needs two companies with an admin each, that is
two `Given` lines, and each line says everything about one company.

The `When` stays one act, even when it has several values. Put them all in its sentence instead of
spreading the act over several `When` steps.

A `Then` can use a table when its rows are one outcome. For example, the seven workflows that a
subscription starts are one outcome. Repeating a step for each workflow would make the scenario
long, so one table lists them all. `Examples` tables follow rules 28 and 29.

## Bad

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1" with:
        | target market | 208 |
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

The one-row table separates a value from an otherwise readable sentence.

```gherkin
Feature: Synchronisation

  Rule: A subscribed data recipient receives the items its data source publishes
    Scenario: A subscribed data recipient receives a newly published item
      Given these companies with their admins:
        | company | kind           | admin |
        | DS1     | data source    | U-DS  |
        | DR1     | data recipient | U-DR  |
      And "U-DR" is subscribed to data source "DS1"
      When "U-DS" publishes a complete item "I1"
      Then "U-DR" sees item "I1" from "DS1" on the Received Items page
```

The table turns the setup of two companies into three columns that the reviewer must read together.

## Good

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1" for target market 208
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

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

Each line names the company, its kind and its admin in one sentence.

```gherkin
Feature: Synchronisation

  Rule: A subscription to a data source starts the delivery workflows
    Scenario: A new subscription creates the delivery workflows
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has published a complete item "I1" for target market 208
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1" for target market 208
      Then the subscription of "DR1" to "DS1" created these workflows on the Workflows page:
        | Subscription from partner             |
        | Subscription from GR                  |
        | Publication from subscription         |
        | CIN from data pool                    |
        | Rdp Synchronization Message Optimizer |
        | CIN to partner                        |
        | Automatic confirmation                |
```

The seven rows are one outcome of the subscription.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- A decision of the sync-autotest suite
