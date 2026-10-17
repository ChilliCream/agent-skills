# Rule 18: Who acts comes first

> Start each step with who or what it is about.

[All rules](../SKILL.md)

## Why

The reader should know at once who acts. If the actor comes at the end, the reader has to read the
whole step first.

For example, write `"C1" adds product "P1" to the cart`, not `product "P1" is added to the cart by
"C1"`. A step about a product starts with the product: `a product "P1" is in stock`.

## Bad

```gherkin
Feature: Shopping cart

  Rule: Customers can add in-stock products to their carts
    Scenario: An in-stock product gets added to a cart
      Given there is a cart belonging to customer "C1"
      And in stock there is a product "P1"
      When product "P1" is added to the cart by "C1"
      Then in the cart product "P1" is visible to "C1"
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
