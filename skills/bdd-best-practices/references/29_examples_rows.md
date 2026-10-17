# Rule 29: Each Examples row is a new case

> Each row of an `Examples` table tests a different case. Give the table a title that says what
> changes.

[All rules](../SKILL.md)

## Why

Choose rows that exercise distinct business cases, boundaries, or risks. Changing an arbitrary
identifier while leaving every relevant condition the same usually repeats a case and adds run
time without useful coverage.

For example, several interchangeable products in stock all illustrate the same cart rule. One
example is enough unless a product difference matters to that rule. For a free-shipping threshold,
values just below, exactly at, and just above the threshold each help detect a boundary mistake.

The table title tells the reader what distinguishes the rows, such as
`Examples: Cart totals around the free-shipping threshold`.

## Bad

```gherkin
Feature: Cart management

  Rule: Products in stock can be added to a cart
    Scenario Outline: A customer adds an available product
      Given a customer "C1" has a cart
      And a product "<product>" is in stock
      When "C1" adds product "<product>" to the cart
      Then "C1" sees product "<product>" in the cart

      Examples:
        | product |
        | P1      |
        | P2      |
        | P3      |
        | P4      |
```

The four identifiers represent the same case, and the table has no descriptive title.

## Good

```gherkin
Feature: Cart management

  Rule: Products in stock can be added to a cart
    Scenario: A customer adds an available product
      Given a customer "C1" has a cart
      And a product "P1" is in stock
      When "C1" adds product "P1" to the cart
      Then "C1" sees product "P1" in the cart
```

Use an Outline when the rows contribute distinct cases:

```gherkin
Feature: Shipping charges

  Rule: Standard shipping costs 5 EUR below 50 EUR and is free from 50 EUR
    Scenario Outline: Shipping charge at the free-shipping threshold
      Given a customer "C1" has a cart totaling <total> EUR
      When "C1" chooses standard shipping
      Then "C1" sees a shipping charge of <charge> EUR

      Examples: Cart totals around the free-shipping threshold
        | total | charge |
        | 49    | 5      |
        | 50    | 0      |
        | 51    | 0      |
```

These rows distinguish below, equal to, and above the threshold while keeping the same steps.

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
