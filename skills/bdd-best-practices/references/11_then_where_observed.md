# Rule 11: A Then checks where the outcome is seen

> Check an outcome through the interface used by its intended recipient: the webshop for a
> customer, a public API response for an API client, or a delivered notification for its recipient.

[All rules](../SKILL.md)

## Why

Check the outcome where the person or system it is for receives it. A check in the database or on
an internal service can pass while the customer still sees the wrong result.

For example, after adding a product to a cart, the customer sees that product in the cart. The
`Then` checks the customer-visible cart, rather than a database row or an internal status code.

If the scenario concerns a public API client, check the public API response. If the outcome is an
order confirmation email, check the email received by the customer. Setup and waiting helpers can
support the scenario, but they cannot substitute for its observable outcome.

## Bad

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the cart database contains a row for "Travel mug" in the customer's cart
      And the internal cart service returns status 200
```

These checks do not prove that the customer sees the product in the cart.

## Good

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

When the business behaviour is offered through a public API, check the result through that API:

```gherkin
Feature: Shopping cart through the public API

  Rule: The public API returns the updated cart after a product is added
    Scenario: A customer adds a product through the public API
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart through the public API
      Then the public API response shows "Travel mug" in the customer's cart
```

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
