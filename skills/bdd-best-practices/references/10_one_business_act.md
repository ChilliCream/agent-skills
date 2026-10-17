# Rule 10: The When is one business act

> The `When` is one act, written the way the business names it.

[All rules](../SKILL.md)

## Why

The `When` is the act that the scenario is about. Write it the way the business would say it, not
as the clicks it takes on the screen.

For example, subscribing to a data source takes several clicks: open the form, pick the data
source, save. In the scenario, it is one step: `"U-DR" subscribes to data source "DS1"`. A partner
company acts the same way, only through a message: `"DS1" sends a valid item message for "I1"`.

Sometimes nobody does anything, because the scenario only checks what a user sees. The act is then
to look: `"U-V" views item "I1"`. Name what the user looks at, not the page that the user opens.

## Bad

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" opens the new subscription form
      And "U-DR" picks data source "DS1"
      And "U-DR" saves the form
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

Three steps describe the screen, but together they are one act.

## Good

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

```gherkin
Feature: Publishing

  Rule: A viewer cannot publish an item
    Scenario: A viewer views a complete item
      Given a data source company "DS1" with a viewer "U-V"
      And "DS1" has a complete item "I1"
      When "U-V" views item "I1"
      Then "U-V" cannot publish item "I1"
```

The viewer does nothing, so the act is to look at the item.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
