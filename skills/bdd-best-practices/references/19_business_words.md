# Rule 19: The words of the business

> Use each project's own business language, and say the same thing the same way every time.

[All rules](../SKILL.md)

## Why

If a scenario uses its own words, a business reader has to translate it. If two steps say the same
thing in different words, a reviewer has to ask whether they mean two different things.

Agree the vocabulary with the people who use the product. In the webshop examples below, they
say customer, product and cart. Another project may use different words. Use its vocabulary
rather than copying the webshop terms.

Once a fact has a clear step phrase, reuse it. For example, `product "Travel mug" is in stock` should
mean the same thing in every webshop scenario that uses it.

Keep the wording plain and simple, the way you explain something to a colleague. The
[ASD-STE100 Simplified Technical English](https://www.asd-ste100.org/) standard is a good guide.

## Bad

```gherkin
Feature: Shopping cart

  Rule: An available entity can join a collection
    Scenario: An account attaches an entity
      Given an account has a collection
      And an entity "Travel mug" is available
      When the account attaches entity "Travel mug" to the collection
      Then the account sees entity "Travel mug" in the collection
```

The technical words hide the customer action and the business objects involved.

## Good

```gherkin
Feature: Shopping cart

  Rule: A customer can add an in-stock product to the cart
    Scenario: A customer adds a product to the cart
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [playwright-bdd](https://vitalets.github.io/playwright-bdd/), keyword matching
