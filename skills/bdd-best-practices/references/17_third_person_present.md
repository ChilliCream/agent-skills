# Rule 17: Third person, present tense

> Write in the third person and the present tense.

[All rules](../SKILL.md)

## Why

A scenario often has several users, so with "I" the reader cannot tell who is speaking. A `Then` in
the future tense ("will see") reads like a later event, not like the result of the act.

For example, write `"C1" adds product "P1" to the cart`, not `I added product "P1" to the cart`. A
`Given` may describe a completed event when it establishes the state now, such as `a customer
"C1" has placed order "O1"`.

## Bad

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: I add a product to my cart
      Given I had a cart
      And a product "P1" was in stock
      When I added product "P1" to my cart
      Then I will see product "P1" in my cart
```

## Good

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: A customer adds an in-stock product to the cart
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [Automation Panda, Should Gherkin steps use first person or third person?](https://automationpanda.com/2017/01/18/should-gherkin-steps-use-first-person-or-third-person/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
