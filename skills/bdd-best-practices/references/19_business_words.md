# Rule 19: The words of the business

> Use each project's own business language, and say the same thing the same way every time.

[All rules](../SKILL.md)

## Why

If a scenario uses its own words, a business reader has to translate it. If two steps say the same
thing in different words, a reviewer has to ask whether they mean two different things.

Agree the vocabulary with the people who use the product. In the webshop examples below, they
say customer, product and cart. Another project may use different words. Use its vocabulary
rather than copying the webshop terms.

Once a fact has a clear step phrase, reuse it. For example, `a product "P1" is in stock` should
mean the same thing in every webshop scenario that uses it.

Keep the wording plain and simple, the way you explain something to a colleague. The
[ASD-STE100 Simplified Technical English](https://www.asd-ste100.org/) standard is a good guide.

## Bad

```gherkin
Feature: Shopping cart

  Rule: An available entity can join a collection
    Scenario: An account attaches an entity
      Given an account "C1" has a collection
      And an entity "P1" is available
      When "C1" attaches entity "P1" to the collection
      Then "C1" sees entity "P1" in the collection
```

The technical words hide the customer action and the business objects involved.

## Good

```gherkin
Feature: Shopping cart

  Rule: A customer can add an in-stock product to the cart
    Scenario: A customer adds a product to the cart
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [playwright-bdd](https://vitalets.github.io/playwright-bdd/), keyword matching
