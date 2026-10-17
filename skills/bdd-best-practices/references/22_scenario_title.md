# Rule 22: A short title that says what happens

> Give each scenario a short title that says who does what.

[All rules](../SKILL.md)

## Why

The title is what a reader scans first and what the test report prints. A long title, or one full
of test words and screen details, hides what the scenario is about.

For example, `A supplier publishes a complete item` says who does what. `Verify that the item
should be published after the user saves the editor` says the same thing with test words and screen
details.

## Bad

```gherkin
Scenario: Verify that the item should be published after the user saves the editor
Scenario: Test that a new subscription shows up in the list on the Subscriptions page
```

## Good

```gherkin
Scenario: A supplier publishes a complete item
Scenario: A data recipient admin subscribes to a data source
```

## Sources

- [Automation Panda, Good Gherkin scenario titles](https://automationpanda.com/2018/01/31/good-gherkin-scenario-titles/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
