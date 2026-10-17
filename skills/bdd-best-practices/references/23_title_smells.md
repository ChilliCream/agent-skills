# Rule 23: A title with "and" may hide two scenarios

> If a title needs "and" or "or", check whether the scenario holds two behaviours. _(smell)_

[All rules](../SKILL.md)

## Why

A title with "and" or "or" often names two behaviours, and two behaviours need two scenarios
(rule 1).

For example, `A supplier creates and publishes an item` names two acts, so it is two scenarios. But
the word alone proves nothing. `A supplier sees the brand name and GTIN of an item` names one thing
that the supplier looks at, so it is fine.

## Bad

```gherkin
Scenario: A supplier creates and publishes an item
Scenario: An item is published or rejected
```

## Good

```gherkin
Scenario: A supplier creates a draft item
Scenario: A supplier publishes a complete item
Scenario: A supplier publishes an item without a brand name
```

Each title names one behaviour.

## Sources

- [Automation Panda, Good Gherkin scenario titles](https://automationpanda.com/2018/01/31/good-gherkin-scenario-titles/)
