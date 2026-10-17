# Rule 19: The words of the business

> Use the words that people in GDSN use, and say the same thing the same way every time.

[All rules](../SKILL.md)

## Why

If a scenario uses its own words, a business reader has to translate it. If two steps say the same
thing in different words, a reviewer has to ask whether they mean two different things.

For example, GDSN says data source, data recipient, item and subscription, not vendor, customer,
product and follow. And `"U-DS" has published a complete item "I1"` always means a live item, so
every scenario that needs a live item uses exactly this step.

Keep the wording plain and simple, the way you explain something to a colleague. The
[ASD-STE100 Simplified Technical English](https://www.asd-ste100.org/) standard is a good guide.

## Bad

```gherkin
Feature: Subscription management

  Rule: A customer can follow a vendor
    Scenario: A customer follows a vendor
      Given a vendor tenant "DS1"
      And a customer tenant "DR1" with a user "U-DR"
      When "U-DR" follows "DS1"
      Then "U-DR" gets the products of "DS1"
```

Nobody in GDSN says tenant, vendor, customer, follow or products.

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

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [playwright-bdd](https://vitalets.github.io/playwright-bdd/) 9.2, keyword matching
