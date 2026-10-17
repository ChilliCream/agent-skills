# Rule 22: A short title that says what happens

> Give each scenario a short title that says who does what.

[All rules](../SKILL.md)

## Why

The title is what a reader scans first and what the test report prints. A long title, or one full
of test words and screen details, hides what the scenario is about.

For example, `A customer adds a product to the cart` says who does what. `Verify that the product
should appear after the user clicks the Add button` adds test words and screen details.

## Bad

```gherkin
Scenario: Verify that the product should appear after the user clicks the Add button
Scenario: Test that the order confirmation shows up after the user clicks Pay
```

## Good

```gherkin
Scenario: A customer adds a product to the cart
Scenario: A customer pays for an order
```

## Sources

- [Automation Panda, Good Gherkin scenario titles](https://automationpanda.com/2018/01/31/good-gherkin-scenario-titles/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
