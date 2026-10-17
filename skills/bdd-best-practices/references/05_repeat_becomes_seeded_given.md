# Rule 5: Test each behaviour in one place

> Test each behaviour directly in its own scenario. Express a result needed as a prerequisite in a
> `Given`.

[All rules](../SKILL.md)

## Why

Keep a scenario focused on its rule. If it needs a result that another scenario already tests,
state that prerequisite in a `Given` instead of repeating the earlier action and its assertions.

For example, checkout needs a product in the customer's cart, but adding products has its own
scenario. State that the cart already contains the product, and check the checkout outcome. This
keeps repeated cart assertions out of checkout scenarios. The setup implementation may still use
shared application code, so a setup failure can affect more than one scenario.

Check an earlier property when preserving it is part of the rule under test. For example, if a
discount must keep the cart quantity unchanged, asserting that quantity after applying the discount
is relevant to that rule.

## Bad

```gherkin
Feature: Checkout

  Rule: A successful checkout confirms the customer's order
    Scenario: A customer checks out
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
      Given the customer is ready to check out
      When the customer checks out
      Then the customer sees the new order as Confirmed in the order history
```

The cart scenario already checks adding a product. Repeating it here introduces a second action
and an assertion that does not describe order confirmation.

## Good

```gherkin
Feature: Checkout

  Rule: A successful checkout confirms the customer's order
    Scenario: A customer checks out
      Given a customer has a cart
      And the customer's cart contains "Travel mug"
      And product "Travel mug" is in stock
      When the customer checks out
      Then the customer sees the new order as Confirmed in the order history
```

The `Given` provides a cart with a product, and the `Then` checks the checkout result.
