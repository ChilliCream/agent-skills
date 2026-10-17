# Rule 4: Every scenario creates its own state

> Every scenario creates everything it needs. It never relies on another scenario.

[All rules](../SKILL.md)

## Why

Scenarios run in parallel and in no fixed order. A scenario that needs another scenario to run
first fails as soon as the order changes.

For example, one scenario completes item "I1", and the next one publishes it. The second scenario
only works if the first one ran before it, in the same worker. Give the second scenario its own
`Given` steps instead, so it creates its own company, user and item.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier completes an item
      Given a data source company "DS1" with an admin "U-DS"
      When "U-DS" completes the checklist of item "I1" with:
        | brand name | Acme |
      Then the checklist of item "I1" shows no errors

    Scenario: A supplier publishes the completed item
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published
```

The second scenario has no `Given`. On its own, it does not know who "U-DS" or "I1" is.

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

- sync-autotest specification guide, _The harness_
- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
