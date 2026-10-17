# Rule 6: A step without an expected result is preparation

> A test case step without an expected result is preparation. Write it as a `Given`, or drop it.

[All rules](../SKILL.md)

## Why

Test cases have many steps that only prepare the real test. Do not copy them into the scenario one
by one.

For example, a test case has these steps:

| Step | Action                        | Expected result       |
| ---- | ----------------------------- | --------------------- |
| 1    | Sign in as the supplier admin |                       |
| 2    | Open the Items page           |                       |
| 3    | Create a complete item        |                       |
| 4    | Publish the item              | The item is Published |

Steps 1 and 2 are a sign-in and a navigation, so drop them. Step 3 prepares the item, so it becomes
`Given "U-DS" has a complete item "I1"`. Step 4 is the act and its result.

There is one exception. If the test case is about a step that has no expected result, do not guess
the outcome. For example, a test case called "Discontinue an item" does not say what happens after
the discontinuation. Write a `Question:` in the behaviour outline and ask.

## Bad

```gherkin
Feature: Publishing

  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" logs in
      And "U-DS" opens the Items page
      And "U-DS" creates a complete item "I1"
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published
```

This scenario copies every step of the test case. The sign-in and the page visit add nothing, and
"creates" tells how the item came to exist instead of saying that it exists.

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

Only the item is left from the preparation, and the `Given` states it as a fact.

## Sources

- sync-autotest specification guide, _Inputs and the behaviour outline_
