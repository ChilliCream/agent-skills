# Rule 5: Test each behaviour in one place

> Do not test the same thing twice. If a scenario needs a result that another scenario tests, put it
> in a `Given`.

[All rules](../SKILL.md)

## Why

Do not test the same thing twice. If your scenario needs something that another scenario already
tests, put it in a `Given` and do not check it again.

For example, you write a scenario for publishing. You need an item to publish, but creating an item
has its own scenario. So you write `Given "U-DS" has a complete item "I1"` and only check what
publishing does. If item creation breaks, only the creation scenario fails, not every scenario that
starts with an item.

There is one exception. If the act you test could break the earlier result, check it. For example,
an item message must keep attribute 816, so the scenario that sends the message checks 816
afterwards.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has a complete item "I1"
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published
      And "U-DS" sees the brand name of item "I1"
```

The creation scenario already checks the brand name, and publishing does not change it.

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

The `Given` provides the item, and the `Then` only checks what publishing does.

## Sources

- A decision of the sync-autotest suite
