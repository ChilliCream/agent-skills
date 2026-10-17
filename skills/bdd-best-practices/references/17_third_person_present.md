# Rule 17: Third person, present tense

> Write in the third person and the present tense.

[All rules](../SKILL.md)

## Why

A scenario often has several users, so with "I" the reader cannot tell who is speaking. A `Then` in
the future tense ("will see") reads like a later event, not like the result of the act.

For example, write `"U-DS" publishes item "I1"`, not `I published item "I1"`. A `Given` may say what
already happened, such as `"U-DS" has published a complete item "I1"`, because it describes the
state now.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: I publish an item
      Given I am the admin of data source company "DS1"
      And I had created a complete item "I1"
      When I published item "I1"
      Then I will see item "I1" on the Items page as Published
```

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

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [Automation Panda, Should Gherkin steps use first person or third person?](https://automationpanda.com/2017/01/18/should-gherkin-steps-use-first-person-or-third-person/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
