# Rule 7: The role that normally does it acts

> Let the role that normally does the act do it. Write a scenario for another role only if that role
> has a user story of its own or gets a different result.

[All rules](../SKILL.md)

## Why

Often several roles can do the same thing. Write the scenario once, for the role that normally does
it.

For example, a data recipient admin normally subscribes to a data source. The super admin can
subscribe for a company too, but that is the same behaviour. So there is one scenario, and the
recipient admin acts in it.

There are two exceptions. Each is a rule of its own, and it gets its own scenario:

- The role has a user story of its own: "As a super admin, I want to manage the subscriptions of
  another company". The result can be the same as for the normal role.
- The role gets a different result. For example, a viewer cannot publish an item.

## Bad

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: The super admin subscribes a data recipient to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" exists
      When the super admin subscribes "DR1" to data source "DS1"
      Then the super admin sees the subscription of "DR1" to "DS1" on the Subscriptions page

    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

The same behaviour twice. The super admin does not normally subscribe for a company, and no user
story asks for it.

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
Feature: Subscription management

  Rule: A super admin can manage the subscriptions of another company
    Scenario: The super admin subscribes a data recipient to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" exists
      When the super admin subscribes "DR1" to data source "DS1"
      Then the super admin sees the subscription of "DR1" to "DS1" on the Subscriptions page
```

The super admin has a user story of its own, so its Rule names that story.

```gherkin
Feature: Publishing

  Rule: A viewer cannot publish an item
    Scenario: A viewer views a complete item
      Given a data source company "DS1" with a viewer "U-V"
      And "DS1" has a complete item "I1"
      When "U-V" views item "I1"
      Then "U-V" cannot publish item "I1"
```

The viewer gets a different result, so the viewer gets a scenario of its own.

## Sources

- A decision of the sync-autotest suite
