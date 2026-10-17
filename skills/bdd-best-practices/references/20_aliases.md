# Rule 20: Name records by an alias

> Use aliases or generated, isolated fixtures for incidental record identifiers. Write concrete
> values when the business rule depends on them.

[All rules](../SKILL.md)

## Why

An identifier such as `"C1"` lets a reader follow the same customer through a scenario without
reading a database ID or an e-mail address. The setup code can map that alias to a generated record
in an isolated fixture. Projects can also use generated fixtures directly when their step
implementation supports that approach. Choose the mechanism the project provides; an alias does
not provide isolation by itself.

Fixed incidental identifiers can couple tests to shared data or collide in parallel runs. Create
isolated data for each scenario, using the project's fixture and cleanup strategy.

Sometimes the value itself is what the rule is about. A scenario about e-mail validation needs the
invalid e-mail, so write it in the step. The attempted customer can still have an alias, while
incidental values come from the fixture.

Scope counts to known data. For example, count products in customer `"C1"`'s isolated cart. A
shop-wide product count is meaningful only when the fixture controls the whole product set.

## Bad

```gherkin
Feature: Shopping cart

  Rule: A customer can add an in-stock product to the cart
    Scenario: A customer adds a product to the cart
      Given a customer "customer-2048@example.com" has a cart
      And a product "product-93746" is in stock
      When "customer-2048@example.com" adds product "product-93746" to the cart
      Then "customer-2048@example.com" sees product "product-93746" in the cart
```

The e-mail address and product ID do not affect the cart rule. Their extra detail hides which
records the reader needs to follow and can tie the scenario to shared fixtures.

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

An e-mail validation scenario needs the invalid value:

```gherkin
Feature: Customer registration

  Rule: A new customer requires a valid e-mail address
    Scenario: A visitor registers with an invalid e-mail address
      Given the shop accepts customer registrations
      When a visitor registers customer "C1" with e-mail "not-an-email"
      Then the visitor sees an invalid e-mail error for customer "C1"
```

The alias `"C1"` names the attempted customer. The value `"not-an-email"` is the invalid input.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
