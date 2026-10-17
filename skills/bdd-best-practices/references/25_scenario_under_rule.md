# Rule 25: Every scenario sits under a Rule

> Put every scenario under a Rule.

[All rules](../SKILL.md)

## Why

A Feature is a capability, such as publishing. A Rule is one business rule of that capability, and
a scenario is an example of that rule. Without the Rule line, the reader sees the example but not
the rule it shows.

A test case is none of these. Do not copy its title or its step numbers into the feature file.
Split it into the examples it contains (rule 6).

## Bad

```gherkin
Feature: Publishing

  Scenario: A supplier publishes a complete item
    Given a data source company "DS1" with an admin "U-DS"
    And "U-DS" has a complete item "I1"
    When "U-DS" publishes item "I1"
    Then "U-DS" sees item "I1" on the Items page as Published
```

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

- sync-autotest specification guide, _The `specs/` tree_
- sync-autotest specification guide, _Inputs and the behaviour outline_
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
