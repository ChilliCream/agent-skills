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
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
      Given "C1" is ready to check out
      When "C1" checks out
      Then "C1" sees order "O1" as confirmed
```

The cart scenario already checks adding a product. Repeating it here introduces a second action
and an assertion that does not describe order confirmation.

## Good

```gherkin
Feature: Checkout

  Rule: A successful checkout confirms the customer's order
    Scenario: A customer checks out
      Given a customer "C1" has a cart
      And the cart of "C1" contains product "P1"
      And a product "P1" is in stock
      When "C1" checks out
      Then "C1" sees order "O1" as confirmed
```

The `Given` provides a cart with a product, and the `Then` checks the checkout result.
