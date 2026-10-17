# Rule 20: Use meaningful names and roles

> Refer to actors by their roles and use memorable names when you need to distinguish them.
> Use descriptive product names and concrete values that explain the business rule.

[All rules](../SKILL.md)

## Why

Readers should follow the business story without decoding identifiers. Choose references that
make the participants and their relationships easy to remember:

- For one actor or object, introduce its role: `a customer has a cart`, then `the customer` and
  `the cart`.
- For multiple actors with the same role, introduce memorable names with their roles: `a customer
  named Alice` and `a customer named Bob`. Use Alice and Bob thereafter.
- For products, use descriptive names such as `"Travel mug"` and `"Coffee beans"`.
- Use concrete quantities, prices and dates when they explain the rule or a boundary case.
- Use code identifiers when their format or identity is itself under test, such as an invalid
  order reference. Otherwise let the fixtures generate incidental database IDs and e-mail addresses.

Choose the form that makes the scenario easiest to follow; a single named actor is also valid.
Keep a role visible when it explains permissions or ownership. Names distinguish participants;
the role and starting facts explain why the business rule applies to them.

Roles and names are narrative references. The step implementation maps them to the scenario's
isolated fixture data; choosing a readable name does not create isolation. Fixtures still create
the required records and handle cleanup independently of other scenarios.

Use realistic synthetic examples. A product called `"Travel mug"` makes the story concrete without
requiring an existing production product with that name. Likewise, Alice in one scenario need not
be the same database record as Alice in another. Create the required records in the fixture and
keep concrete values that help explain the rule. Rule 21 explains which details the business rule
needs.

## Bad

```gherkin
Feature: Shopping cart

  Rule: A customer can add an in-stock product to the cart
    Scenario: A customer adds a product to the cart
      Given a customer with e-mail "alice.smith@example.com" has a cart
      And a product with database ID 93746 is in stock
      When the customer with e-mail "alice.smith@example.com" adds product 93746 to the cart
      Then the customer with e-mail "alice.smith@example.com" sees product 93746 in the cart
```

The e-mail and database ID do not explain the cart rule. The reader must decode technical
references instead of following a customer buying a familiar product.

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

One customer can be identified by their role alone. When two customers have the same role,
names make their separate carts easy to follow:

```gherkin
Feature: Cart ownership

  Rule: Adding a product changes only the customer's own cart
    Scenario: A customer's addition leaves another customer's cart empty
      Given a customer named Alice has an empty cart
      And a customer named Bob has an empty cart
      And product "Travel mug" is in stock
      When Alice adds "Travel mug" to her cart
      Then Alice sees "Travel mug" in her cart
      And Bob sees an empty cart
```

An e-mail validation rule needs the concrete invalid input:

```gherkin
Feature: Customer registration

  Rule: A new customer requires a valid e-mail address
    Scenario: A visitor registers with an invalid e-mail address
      Given the shop accepts customer registrations
      When a visitor registers with e-mail "not-an-email"
      Then the visitor sees an error that the e-mail address is invalid
```

The invalid value explains the rejection. An invented customer identifier would add no meaning.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Cucumber, Tips for using Background](https://cucumber.io/docs/gherkin/reference/#tips-for-using-background)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
