# Rule 10: The When is one business act

> The `When` is one act, written the way the business names it.

[All rules](../SKILL.md)

## Why

The `When` is the act that the scenario is about. Write it the way the business would say it, not
as the clicks it takes on the screen.

For example, adding a product to a cart may take several interactions: open the product, choose a
quantity and press a button. In the scenario, it is one step: `"C1" adds product "P1" to the cart`.
The automation implements the interactions behind that step.

A scenario about information shown to a customer can use viewing as its act: `"C1" views product
"P1"`. Name the business object the customer looks at, rather than the navigation used to reach it.

## Bad

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" opens the page for product "P1"
      And "C1" selects a quantity of 1
      And "C1" presses the Add to cart button
      Then "C1" sees product "P1" in the cart
```

Three steps describe the screen, but together they are one business act.

## Good

```gherkin
Feature: Shopping cart

  Rule: An in-stock product can be added to a cart
    Scenario: A customer adds an in-stock product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

```gherkin
Feature: Product availability

  Rule: An out-of-stock product is shown as unavailable
    Scenario: A customer views an out-of-stock product
      Given a customer "C1" exists
      And product "P1" is out of stock
      When "C1" views product "P1"
      Then "C1" sees product "P1" as Out of stock
```

Viewing the product is the act that exposes its availability to the customer.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
