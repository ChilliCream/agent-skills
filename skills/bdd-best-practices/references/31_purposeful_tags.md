# Rule 31: Give tags a documented purpose

> Give each tag a documented purpose. Follow the project's runner configuration and tag policy.

[All rules](../SKILL.md)

## Why

Tags can select scenarios for a run, connect them to requirements or enable runner features.
Their names and effects depend on the project. A tag does not inherently skip a scenario, set a
timeout or link an issue; the runner and its configuration determine that behaviour.

Add a tag when a documented selection rule, integration or traceability need uses it. Keep the
business rule and outcome in the scenario, so readers can understand it without interpreting tags.

## Bad

```gherkin
Feature: Checkout

  Rule: Checkout creates an order for the products in the cart
    @fast @important @customer @cart @order @checkout
    Scenario: A customer checks out
      Given a customer "C1" has a cart ready for checkout
      When "C1" checks out
      Then "C1" sees an order confirmation for the products in the cart
```

Assume none of these tags has a documented consumer. They repeat the scenario's subject or make
claims such as "fast" without defining how the test run uses them.

## Good

```gherkin
Feature: Checkout

  Rule: Checkout creates an order for the products in the cart
    @checkout
    Scenario: A customer checks out
      Given a customer "C1" has a cart ready for checkout
      When "C1" checks out
      Then "C1" sees an order confirmation for the products in the cart
```

In this example, the project's test configuration selects `@checkout` for its checkout checks.
Another project may use a different tag or select scenarios by path. Use its documented policy;
there is no universal required set of tags.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Cucumber, Tags](https://cucumber.io/docs/cucumber/api/#tags)
