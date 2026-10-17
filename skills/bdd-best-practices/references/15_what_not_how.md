# Rule 15: What, not how

> Write what the user does, not how the user works the screen.

[All rules](../SKILL.md)

## Why

A step that describes the screen breaks every time the screen changes, even when the behaviour
stays the same.

For example, `"C1" adds product "P1" to the cart` stays correct when the product page changes. A
step like `"C1" scrolls to the purchase panel and clicks the green Add button` has to change. Keep
screen mechanics such as clicks, selectors and tab order in the step implementation. A page name
is useful when it identifies where the customer observes an outcome.

Keep an interface detail when it is the behaviour under test, such as keyboard navigation or an
accessible control name. The rule removes incidental mechanics, not the feature being specified.

Name a public API, message or file when the scenario tests that interface (rule 11). Keep
endpoint paths, HTTP commands and transport setup in the bindings.

## Bad

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: A customer adds an in-stock product to the cart
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" scrolls to the purchase panel for "P1" and clicks the green Add button
      Then the second row of the cart table contains "P1"
```

If the panel, button or table layout changes, the steps change even though adding a product did
not.

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

- [Cucumber, Writing better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/)
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
