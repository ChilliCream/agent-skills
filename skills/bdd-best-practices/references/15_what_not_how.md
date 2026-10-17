# Rule 15: What, not how

> Write what the user does, not how the user works the screen.

[All rules](../SKILL.md)

## Why

A step that describes the screen breaks every time the screen changes, even when the behaviour
stays the same.

For example, `"U-DS" publishes item "I1"` stays correct when the item editor gets new tabs. A step
like `"U-DS" opens the item editor and saves the Core data tab` has to change. So words such as
click, button, tab, scroll or upload do not belong in a step. Page names are fine, because the
business talks about the Items page too.

Name a public API, message or file when the scenario tests that interface (rule 11). Keep
endpoint paths, HTTP commands and transport setup in the bindings.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has a complete item "I1"
      When "U-DS" opens the item editor of "I1" and saves the Core data tab with publishing on
      Then the status column of "I1" reads "PUBLISHED"
```

If the editor's tabs or the column change, both steps change, although publishing did not.

## Good

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has a complete item "I1"
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published
```

## Sources

- [Cucumber, Writing better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/)
- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
