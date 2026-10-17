# Rule 20: Name records by an alias

> Give everything that a scenario creates an alias, such as `"DS1"`, `"U-DS"` or `"I1"`. Never
> write a real name, ID or e-mail for a record. Write a concrete value only when the rule depends on
> that value.

[All rules](../SKILL.md)

## Why

The automation creates new test data in every run, and GDSN never deletes it. A fixed ID or e-mail
would collide with the one from the previous run. With an alias, the scenario stays the same, and
the automation creates new real values for it in each run.

Sometimes the value itself is what the rule is about. A scenario about e-mail validation needs the
invalid e-mail, so write it in the step. The user still gets an alias, and the automation still
creates every other value.

This also affects counts. A count is only reliable for the records that your scenario created. For
example, you can check that the Items page lists item `"I1"` once. You cannot check that the super
admin sees 12 subscriptions, because earlier runs created subscriptions too.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "Acme Foods A/S" with an admin "jens@acme.dk"
      And "jens@acme.dk" has a complete item "5701234567892"
      When "jens@acme.dk" publishes item "5701234567892"
      Then "jens@acme.dk" sees item "5701234567892" on the Items page as Published
```

On the second run, this e-mail and this GTIN already exist in GDSN.

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

An e-mail validation scenario needs the invalid value:

```gherkin
Feature: User management

  Rule: A new user requires a valid e-mail address
    Scenario: The super admin enters an invalid e-mail address for a new user
      Given a data source company "DS1" exists
      When the super admin creates user "U1" for company "DS1" with e-mail "not-an-email"
      Then the super admin sees an invalid e-mail error for user "U1"
```

The alias `"U1"` names the attempted user. The value `"not-an-email"` is the invalid input.

## Sources

- sync-autotest specification guide, _The harness_
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
