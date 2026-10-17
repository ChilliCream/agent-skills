# Rule 6: Supporting actions become preparation

> Express supporting actions as the state they prepare, or omit them when the rule does not need
> them. Clarify the expected result of the action under test.

[All rules](../SKILL.md)

## Why

Test cases have many steps that only prepare the real test. Do not copy them into the scenario one
by one.

For example, a test case has these steps:

| Step | Action                    | Expected result                  |
| ---- | ------------------------- | -------------------------------- |
| 1    | Sign in as a customer     |                                  |
| 2    | Open the product page     |                                  |
| 3    | Create an empty cart      |                                  |
| 4    | Add an available product  | The product appears in the cart  |

For this cart rule, steps 1 and 2 are incidental sign-in and navigation, so omit them. Step 3
prepares the cart, so it becomes `Given a customer "C1" has a cart`. Make the product's availability
explicit as another prerequisite. Step 4 is the act and its result.

An empty expected-result cell alone does not prove that an action is preparation. If the test case
is about that action, clarify its outcome before writing a `Then`. For example, a test case called
"Cancel an order" must explain what the customer can observe after cancellation. Record that
question rather than guessing whether the order disappears, changes status or receives a refund.

## Bad

```gherkin
Feature: Cart management

  Rule: Customers can add products that are in stock
    Scenario: A customer adds a product
      Given a customer "C1" exists
      And "C1" logs in
      And "C1" opens the page for product "P1"
      And "C1" creates an empty cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

This scenario copies every step of the test case. The sign-in and the page visit add nothing, and
"creates" tells how the cart came to exist instead of saying that it exists.

## Good

```gherkin
Feature: Cart management

  Rule: Customers can add products that are in stock
    Scenario: A customer adds a product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

Only the necessary preparation remains, and the `Given` steps state it as facts.
