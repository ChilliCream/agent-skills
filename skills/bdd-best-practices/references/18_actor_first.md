# Rule 18: Who acts comes first

> Start each step with who or what it is about.

[All rules](../SKILL.md)

## Why

The reader should know at once who acts. If the actor comes at the end, the reader has to read the
whole step first.

For example, write `"U-DS" publishes item "I1"`, not `item "I1" is published by "U-DS"`. A step
about a record starts with the record: `a data source company "DS1" exists`.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A complete item gets published
      Given there is a complete item "I1" owned by "U-DS" of data source company "DS1"
      When item "I1" is published by "U-DS"
      Then on the Items page item "I1" shows as Published for "U-DS"
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

- A decision of the sync-autotest suite
