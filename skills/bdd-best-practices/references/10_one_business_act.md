# Rule 10: The When is one business act

> The `When` is one act, written the way the business names it.

[All rules](../SKILL.md)

## Why

The `When` is the act that the scenario is about. Write it the way the business would say it, not
as the clicks it takes on the screen.

For example, adding a product to a cart may take several interactions: open the product, choose a
quantity and press a button. In the scenario, it is one step:
`the customer adds "Travel mug" to the cart`. The automation implements the interactions behind
that step.

A scenario about information shown to a customer can use viewing as its act:
`the customer views "Travel mug"`. Name the business object the customer looks at, rather than the
navigation used to reach it.

## Bad

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer opens the page for "Travel mug"
      And the customer selects a quantity of 1
      And the customer presses the Add to cart button
      Then the customer sees "Travel mug" in the cart
```

Three steps describe the screen, but together they are one business act.

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

```gherkin
Feature: Product availability

  Rule: An out-of-stock product is shown as unavailable
    Scenario: A customer views an out-of-stock product
      Given product "Travel mug" is out of stock
      When a customer views "Travel mug"
      Then the customer sees "Travel mug" as Out of stock
```

Viewing the product is the act that exposes its availability to the customer.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
