---
name: bdd-best-practices
description: Gherkin and BDD scenario rules, 31 numbered rules with bad and good examples. Use when writing, reviewing or refactoring Gherkin scenarios, feature files or step phrases, when asked whether a scenario follows BDD best practices, or when a rule is cited by number ("breaks rule 13").
---

# BDD best practices

These rules govern the text of a Gherkin scenario. A scenario is documentation first: a business
reader understands it without the code. Seb Rose: "Scenarios should be thought of as
documentation, rather than tests."

The examples and conventions marked **G** come from the sync-autotest GDSN suite. In another
project, use that project's agreed domain vocabulary, harness, commands and tag policy. Apply
GDSN-specific conventions only where they match that project's practice.

## Using the rules

- **Writing:** apply every rule below to each scenario before you show it.
- **Reviewing:** check each scenario against each rule. Before you report a broken rule, read its
  page in `references/`. Cite the rule by its number, for example "breaks rule 13".
- A rule marked _(smell)_ asks for an examination, not a fix. Report it as a note.

Each rule links to its page. The page says why the rule holds and shows a bad example, a good
example and the sources. The examples come from GDSN (data sources, data recipients, items,
subscriptions). They show the wording; their steps have no bindings.

## Example

Rewrite setup as an existing fact (rule 9).

Before:

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

After:

```gherkin
Feature: Publishing
  Rule: Publishing a complete item makes it Published
    Scenario: A supplier publishes a complete item
      Given a data source company "DS1" with an admin "U-DS"
      And "U-DS" has a complete item "I1"
      When "U-DS" publishes item "I1"
      Then "U-DS" sees item "I1" on the Items page as Published
```

The scenario needs a complete item; its creation belongs in the setup implementation.

## Rules

### Behaviour

1. [Test one behaviour in each scenario. A second act belongs in a second scenario.](references/01_one_behaviour.md) (G, P)
2. [If one act has consequences under different rules, write one scenario for each rule.](references/02_split_by_rule.md) (G, R)
3. [If the same act starts from a different state, write a separate scenario.](references/03_different_starting_state.md) (G)
4. [Every scenario creates everything it needs. It never relies on another scenario.](references/04_own_state.md) (G, P)
5. [Do not test the same thing twice. If a scenario needs a result that another scenario tests, put it in a `Given`.](references/05_repeat_becomes_seeded_given.md) (G)
6. [A test case step without an expected result is preparation. Write it as a `Given`, or drop it.](references/06_action_without_result_is_setup.md) (G)
7. [Let the role that normally does the act do it. Write a scenario for another role only if that role has a user story of its own or gets a different result.](references/07_owning_role_acts.md) (G)

### Steps

8. [First the `Given` steps, then one `When`, then the `Then` steps.](references/08_given_when_then_order.md) (G, P)
9. [A `Given` says what is already there, not how it got there.](references/09_given_states_a_fact.md) (G, C, R)
10. [The `When` is one act, written the way the business names it.](references/10_one_business_act.md) (G, C, N)
11. [Check an outcome where the person or company it is for gets it: the page for a user, the public API response, the message or the file for a partner company.](references/11_then_where_observed.md) (G, C)
12. [A `Then` checks the outcome itself, not a sign of it.](references/12_then_proves_the_outcome.md) (G, P)
13. [Put one fact in each step.](references/13_one_fact_per_step.md) (G, A)
14. [Aim for about five steps. If a scenario has more, look for a second behaviour or a detail that the rule does not need. _(smell)_](references/14_keep_it_brief.md) (C, R, G)

### Wording

15. [Write what the user does, not how the user works the screen.](references/15_what_not_how.md) (B, C, G, R)
16. [Do not write a sign-in step. Each step defines which actor does it.](references/16_no_sign_in.md) (G, B)
17. [Write in the third person and the present tense.](references/17_third_person_present.md) (P, N, G)
18. [Start each step with who or what it is about.](references/18_actor_first.md) (G)
19. [Use the words that people in GDSN use, and say the same thing the same way every time.](references/19_business_words.md) (C, R, N, G, P)
20. [Give everything that a scenario creates an alias, such as `"DS1"`, `"U-DS"` or `"I1"`. Never write a real name, ID or e-mail for a record. Write a concrete value only when the rule depends on that value.](references/20_aliases.md) (G)
21. [Leave out every value and every step that the rule does not need.](references/21_no_incidental_detail.md) (G, R, B)

### Titles

22. [Give each scenario a short title that says who does what.](references/22_scenario_title.md) (P, N, G)
23. [If a title needs "and" or "or", check whether the scenario holds two behaviours. _(smell)_](references/23_title_smells.md) (P)
24. [Write a Rule title as a sentence that can be true or false.](references/24_rule_title.md) (G, C)

### Structure

25. [Put every scenario under a Rule.](references/25_scenario_under_rule.md) (G, C)
26. [Put a scenario in the feature of the capability it is about, not of the screen it uses.](references/26_capability_placement.md) (G)
27. [A `Background` holds only the `Given` steps that every scenario below it needs.](references/27_background.md) (G, C)
28. [Use a Scenario Outline only when every row runs the same steps and only the values change.](references/28_outline_same_rule.md) (G, P, C)
29. [Each row of an `Examples` table tests a different case. Give the table a title that says what changes.](references/29_examples_rows.md) (P, G)
30. [Do not use a table in a `Given` or a `When`. Write values in the step sentence and a list as separate lines. Use a table only for a `Then` whose rows are one outcome.](references/30_with_table.md) (G)

### Tags

31. [Use only three tags: `@fixme`, `@timeout:<ms>` and `@TC-<id>`.](references/31_three_tags.md) (G)

## Sources

The letters after each rule name its sources:

- **G**: a decision of the sync-autotest suite
- **C**: [Cucumber Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- **B**: [Cucumber, Writing better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/)
- **R**: [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
- **P**: [Automation Panda](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- **N**: [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
- **A**: [Cucumber anti-patterns](https://cucumber.io/docs/guides/anti-patterns/)
