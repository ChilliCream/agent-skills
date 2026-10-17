# Rule 1: One behaviour per scenario

> Test one behaviour in each scenario. A second act belongs in a second scenario.

[All rules](../SKILL.md)

## Why

A scenario that tests several behaviours one after the other stops at the first failure. You then
do not know whether the rest still works.

For example, a scenario creates an item, publishes it and then withdraws it. If publishing breaks,
the scenario stops there, and nobody learns whether withdrawing still works. Write one scenario for
each behaviour instead. The withdrawal scenario simply starts with an item that is already published
(rule 5).

## Bad

```gherkin
Feature: Items

  Rule: Items have a lifecycle
    Scenario: Item lifecycle
      Given a data source company "DS1" with an admin "U-DS"
      When "U-DS" creates a draft item "I1"
      Then "U-DS" sees item "I1" on the Items page as Draft
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published
      When "U-DS" withdraws item "I1"
      Then "U-DS" sees item "I1" on the Items page as Withdrawn
```

Three behaviours in one scenario. If publishing fails, the withdrawal is never tested.

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

```gherkin
Feature: Withdrawal

  Rule: Withdrawing a published item makes it Withdrawn
    Scenario: A supplier withdraws a published item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has published a complete item "I1"
      When "U-DS" withdraws item "I1"
      Then "U-DS" sees item "I1" on the Items page as Withdrawn
```

Each behaviour has its own scenario, and each one starts from the state it needs.

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [Automation Panda, Are Gherkin scenarios with multiple When-Then pairs okay?](https://automationpanda.com/2018/02/03/are-gherkin-scenarios-with-multiple-when-then-pairs-okay/)
