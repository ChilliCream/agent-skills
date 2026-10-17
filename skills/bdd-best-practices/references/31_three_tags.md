# Rule 31: Three tags only

> Use only three tags: `@fixme`, `@timeout:<ms>` and `@TC-<id>`.

[All rules](../SKILL.md)

## Why

`@fixme` marks a scenario that fails because GDSN is wrong. The run skips it, and a comment above
the tag names the defect. `@timeout:<ms>` gives a slow scenario more time, for example one that
waits for a workflow.

`@TC-<id>` links a scenario to its Azure DevOps case, and `yarn backlog` reads it to track which
cases are automated. Several focused scenarios may share a case tag. The link does not decide their
wording or capability folder.

Do not add tags for suites or smoke runs. To run a part of the suite, run its folder, for example
`yarn test specs/catalogue`.

## Bad

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    @smoke @regression
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

A folder selects a part of the suite, so these tags only repeat it.

## Good

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    @TC-16457
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then "U-DR" sees the subscription to "DS1" on the Subscriptions page
```

```gherkin
Feature: Recipient sharing

  Rule: A recipient sees the received items of the companies that share with it
    # Defect: the filter box lists DR2 twice once DR1 has shared with it
    @fixme
    Scenario: A recipient filters by the company that shares its items
      Given a data recipient company "DR1" with an admin "U-DR1"
      And a data recipient company "DR2" with an admin "U-DR2"
      And "DR1" shares its received items with "DR2"
      When "U-DR2" chooses the received items of "DR1"
      Then "U-DR2" sees only the received items of "DR1"
```

## Sources

- [playwright-bdd](https://vitalets.github.io/playwright-bdd/) 9.2, special tags
- sync-autotest specification guide, _Runs_
