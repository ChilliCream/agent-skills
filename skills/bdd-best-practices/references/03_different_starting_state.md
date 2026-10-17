# Rule 3: A different starting state is a different scenario

> If the same act starts from a different state, write a separate scenario.

[All rules](../SKILL.md)

## Why

The same act can start from different states. For example, a supplier can publish a complete item
or an item without a brand name. Each starting state gets its own scenario, so a failure shows
which case broke.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes items
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has a complete item "I1"
      And "U-DS" has an item "I2" without a brand name
      When "U-DS" publishes items "I1" and "I2"
      Then "U-DS" sees item "I1" on the Items page as Published
      And "U-DS" sees that item "I2" misses its brand name
```

One scenario starts from two states. If the check of `I1` fails, the run never checks `I2`.

## Good

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has a complete item "I1"
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published

  Rule: A supplier can only publish an item that has a brand name
    Scenario: A supplier publishes an item without a brand name
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has an item "I1" without a brand name
      When "U-DS" publishes item "I1"
      Then "U-DS" sees that item "I1" misses its brand name
```

Each scenario starts from one state, and its title names that state.

## Sources

- A decision of the sync-autotest suite
