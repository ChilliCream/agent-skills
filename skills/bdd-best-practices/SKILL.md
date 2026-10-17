---
name: bdd-best-practices
description: BDD best practices for writing, reviewing and refactoring Gherkin scenarios, feature files and step phrases, with 31 numbered rules and webshop examples. Use when working on .feature files, checking whether a scenario follows BDD best practices, or discussing a rule by number ("breaks rule 13").
---

# BDD best practices

Write Gherkin scenarios that a business reader understands without reading the test code.
The examples use a webshop with customers, products, carts and orders. Apply the same principles
to the vocabulary and observable outcomes of the project you are working on.

## Using the rules

- **Writing:** check each scenario against the rules below before you show it.
- **Reviewing:** check each scenario against each rule. Before you report a broken rule, read its
  page in `references/`. Cite the rule by its number, for example "breaks rule 13".
- A rule marked _(smell)_ asks for an examination, not a fix. Report it as a note.

Each rule links to its explanation and bad and good examples. Read the relevant page when a
rule needs clarification. The example steps illustrate wording; they have no bindings. Follow
the project's conventions for fixtures, authentication and runner configuration.

## Example

Rewrite setup as an existing fact (rule 9).

Before:

```gherkin
Feature: Cart management
  Rule: A customer can add an in-stock product to the cart
    Scenario: A customer adds an in-stock product
      Given a visitor opens the registration page
      And the visitor registers as a customer
      And the customer creates an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

After:

```gherkin
Feature: Cart management
  Rule: A customer can add an in-stock product to the cart
    Scenario: A customer adds an in-stock product
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

The scenario needs a customer with a cart; their creation belongs in the setup implementation.

## Rules

### Behaviour

1. [Test one behaviour in each scenario. A second act belongs in a second scenario.](references/01_one_behaviour.md)
2. [If one act has consequences under different rules, write one scenario for each rule.](references/02_split_by_rule.md)
3. [If the same act starts from a different state, write a separate scenario.](references/03_different_starting_state.md)
4. [Declare each scenario's starting state independently. Never rely on another scenario.](references/04_own_state.md)
5. [Test each behaviour directly in its own scenario. Use a `Given` when another scenario only needs its result as a prerequisite.](references/05_repeat_becomes_seeded_given.md)
6. [Move preparatory actions into `Given` steps or remove them. If an action is the behaviour under test, establish its expected result.](references/06_action_without_result_is_setup.md)
7. [Let the role that normally does the act do it. Write a scenario for another role when its behaviour, permissions or result differs.](references/07_owning_role_acts.md)

### Steps

8. [First the `Given` steps, then one `When`, then the `Then` steps.](references/08_given_when_then_order.md)
9. [A `Given` says what is already there, not how it got there.](references/09_given_states_a_fact.md)
10. [The `When` is one act, written the way the business names it.](references/10_one_business_act.md)
11. [Check an outcome at the boundary where its intended user or external system observes it.](references/11_then_where_observed.md)
12. [A `Then` checks the outcome itself, not a sign of it.](references/12_then_proves_the_outcome.md)
13. [Put one fact in each step.](references/13_one_fact_per_step.md)
14. [Aim for about five steps. If a scenario has more, look for a second behaviour or a detail that the rule does not need. _(smell)_](references/14_keep_it_brief.md)

### Wording

15. [Write what the user does, not how the user works the screen.](references/15_what_not_how.md)
16. [Treat authentication as setup unless signing in is the behaviour under test.](references/16_no_sign_in.md)
17. [Write in the third person and the present tense.](references/17_third_person_present.md)
18. [Start each step with who or what it is about.](references/18_actor_first.md)
19. [Use the project's business vocabulary consistently.](references/19_business_words.md)
20. [Use meaningful names and roles. Name entities when it helps distinguish them, and use concrete values when the rule depends on them.](references/20_meaningful_names.md)
21. [Leave out every value and every step that the rule does not need.](references/21_no_incidental_detail.md)

### Titles

22. [Give each scenario a short title that says who does what.](references/22_scenario_title.md)
23. [If a title needs "and" or "or", check whether the scenario holds two behaviours. _(smell)_](references/23_title_smells.md)
24. [Write a Rule title as a sentence that can be true or false.](references/24_rule_title.md)

### Structure

25. [Group scenarios under a named `Rule` when the runner supports it and the grouping makes the business rule clearer.](references/25_scenario_under_rule.md)
26. [Put a scenario in the feature of the capability it is about, not of the screen it uses.](references/26_capability_placement.md)
27. [A `Background` holds only the `Given` steps that every scenario in its scope needs.](references/27_background.md)
28. [Use a Scenario Outline only when every row runs the same steps and only the values change.](references/28_outline_same_rule.md)
29. [Each row of an `Examples` table tests a different case. Give the table a title that says what changes.](references/29_examples_rows.md)
30. [Use data tables for coherent business facts or collections in a `Given`, `When` or `Then`. Keep unrelated details and hidden actions out.](references/30_with_table.md)

### Tags

31. [Give each tag a documented purpose. Follow the project's runner configuration and tag policy.](references/31_purposeful_tags.md)

## Supporting readability check

BRIEF is an optional reminder of writing principles covered by the rules above:

- **Business language:** use the project's agreed vocabulary (rule 19).
- **Real data:** use meaningful examples and concrete values that reveal the rule (rules 20, 21, 29).
- **Intention revealing:** make the business action and its outcome clear (rules 10, 12, 15, 22).
- **Essential:** keep the facts and details this rule needs (rules 6, 21).
- **Focused:** illustrate one behaviour under one business rule (rules 1, 2, 5).
- **Brief:** keep the example easy to read without losing necessary facts (rule 14).

## Sources

- [Cucumber Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
- [Cucumber, Writing better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/)
- [Seb Rose, Keep your scenarios BRIEF](https://cucumber.io/blog/bdd/keep-your-scenarios-brief/)
- [Automation Panda](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
- [Dan North, Introducing BDD](https://dannorth.net/introducing-bdd/)
- [Cucumber anti-patterns](https://cucumber.io/docs/guides/anti-patterns/)
