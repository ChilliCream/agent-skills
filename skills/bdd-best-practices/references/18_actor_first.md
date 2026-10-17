# Rule 18: Who acts comes first

> Start each step with who or what it is about.

[All rules](../SKILL.md)

## Why

The reader should know at once who acts. If the actor comes at the end, the reader has to read the
whole step first.

For example, write `the customer adds "Travel mug" to the cart`, not `"Travel mug" is added to
the cart by the customer`. A step about a product starts with the product: `product "Travel mug"
is in stock`.

## Bad

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: An in-stock product gets added to a cart
      Given there is an empty cart belonging to a customer
      And in stock there is product "Travel mug"
      When "Travel mug" is added to the cart by the customer
      Then in the cart "Travel mug" is visible to the customer
```

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
