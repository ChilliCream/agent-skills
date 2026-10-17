# Rule 9: A Given says what is there

> A `Given` says what is already there, not how it got there.

[All rules](../SKILL.md)

## Why

A `Given` describes how the world looks when the scenario starts. It does not tell the story of how
it got that way.

For example, your scenario needs an item that is ready to publish. Write
`"U-DS" has a complete item "I1"`. Do not write that the user opened the Items page, created a
draft and filled in the mandatory fields. Those steps make the scenario long, and they break it as
soon as publishing needs one more field, even though the thing you test did not change.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" opens the Items page
      And "U-DS" creates a draft item "I1"
      And "U-DS" fills in the mandatory fields of item "I1"
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published
```

Three steps tell how the item was made. The scenario only needs to know that it exists and is
complete.

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

One `Given` says that the item exists and is complete. The step decides what "complete" means.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
