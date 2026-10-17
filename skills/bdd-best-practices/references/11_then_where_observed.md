# Rule 11: A Then checks where the outcome is seen

> Check an outcome where the person or company it is for gets it: the page for a user, the public
> API response, the message or the file for a partner company.

[All rules](../SKILL.md)

## Why

Check the outcome where the one it is for gets it. A check in the database or on an internal API
can pass while that person or company still gets nothing.

For example, a data recipient admin subscribes to a data source. The admin sees the new
subscription on the Subscriptions page, so the `Then` checks that page, not a database table or a
status code. If the scenario says a user sees an item, check the page.

A partner company that integrates through the public API gets the outcome in the API response.
Then the `Then` checks that response. For a partner that exchanges messages or files, the outcome
is the message or the file that it gets back. For work inside GDSN, such as a processed message,
the outcome is on the Workflows page. API and database helpers may prepare data or wait for
processing, but they never replace the outcome.

## Bad

```gherkin
Feature: Subscription management

  Rule: A subscription targets one data source in one target market
    Scenario: A data recipient admin subscribes to a data source
      Given a data source company "DS1" exists
      And a data recipient company "DR1" with an admin "U-DR"
      When "U-DR" subscribes to data source "DS1"
      Then the subscriptions table holds a row for "DR1" and "DS1"
      And the subscription service answers 200
```

These checks do not prove that the recipient admin sees the subscription on the Subscriptions page.

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

A data source that publishes through the public API gets the result in the API response:

```gherkin
Feature: Publishing through the public API

  Rule: The public API reports the result of a publication to the data source
    Scenario: A data source publishes a complete item through the public API
      Given a data source company "DS1" exists
      And "DS1" has a complete item "I1"
      When "DS1" publishes item "I1" through the public API
      Then "DS1" sees item "I1" as published in the request status of the public API
```

## Sources

- sync-autotest specification guide, _Inputs and the behaviour outline_
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
