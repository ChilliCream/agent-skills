# Rule 16: No sign-in step

> Do not write a sign-in step. Each step defines which actor does it.

[All rules](../SKILL.md)

## Why

Each step defines which actor does it. For example, in `"U-DS" publishes item "I1"`, the actor is
`"U-DS"`. The test signs in as that actor by itself, so a sign-in step adds nothing. It only makes
the scenario longer.

There is one exception: a scenario about signing in or signing out. There the sign-in is the act
that the scenario tests, so it is the `When`.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has a complete item "I1"
      And "U-DS" logs in
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

```gherkin
Feature: Sign-in and SSO

  Rule: Signing out ends the session
    Scenario: A user signs out
      Given a data recipient company "DR1" with an admin "U-DR"
      And "U-DR" is signed in
      When "U-DR" signs out
      Then "U-DR" is asked to sign in when opening its received items
```

Here signing out is the behaviour, so it is the `When`.

## Sources

- sync-autotest specification guide, _The harness_
- [Cucumber, Writing better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/)
