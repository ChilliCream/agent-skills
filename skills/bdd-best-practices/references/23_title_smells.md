# Rule 23: A title with "and" may hide two scenarios

> If a title needs "and" or "or", check whether the scenario holds two behaviours. _(smell)_

[All rules](../SKILL.md)

## Why

A title with "and" or "or" often names two behaviours, and two behaviours need two scenarios
(rule 1).

For example, `A customer adds a product and pays for an order` names two acts, so it is two
scenarios. But the word alone proves nothing. `A customer sees the name and price of a product`
describes one product summary, so it can be one behaviour.

## Bad

```gherkin
Scenario: A customer adds a product and pays for an order
Scenario: A payment is accepted or declined
```

## Good

```gherkin
Scenario: A customer adds a product to the cart
Scenario: A customer pays for an order with a valid card
Scenario: A customer attempts payment with a declined card
```

Each title names one behaviour.

## Sources

- [Automation Panda, Good Gherkin scenario titles](https://automationpanda.com/2018/01/31/good-gherkin-scenario-titles/)
