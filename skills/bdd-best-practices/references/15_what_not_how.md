# Rule 15: What, not how

> Write what the user does, not how the user works the screen.

[All rules](../SKILL.md)

## Why

A step that describes the screen breaks every time the screen changes, even when the behaviour
stays the same.

For example, `the customer adds "Travel mug" to the cart` stays correct when the product page
changes. A step like `the customer scrolls to the purchase panel and clicks the green Add button`
has to change. Keep screen mechanics such as clicks, selectors and tab order in the step
implementation. A page name is useful when it identifies where the customer observes an outcome.

Keep an interface detail when it is the behaviour under test, such as keyboard navigation or an
accessible control name. The rule removes incidental mechanics, not the feature being specified.

Name a public API, message or file when the scenario tests that interface (rule 11). Keep
endpoint paths, HTTP commands and transport setup in the bindings.

## Bad

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: A customer adds an in-stock product to the cart
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer scrolls to the purchase panel for "Travel mug" and clicks the green Add button
      Then the first row of the cart table contains "Travel mug"
```

If the panel, button or table layout changes, the steps change even though adding a product did
not.

## Good

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: A customer adds an in-stock product to the cart
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

## Sources

- [Cucumber, Writing better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/)
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
