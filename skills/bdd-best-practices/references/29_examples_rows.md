# Rule 29: Each Examples row is a new case

> Each row of an `Examples` table tests a different case. Give the table a title that says what
> changes.

[All rules](../SKILL.md)

## Why

Choose rows that exercise distinct business cases, boundaries, or risks. Changing a product name
while leaving every relevant condition the same usually repeats a case and adds run time without
useful coverage.

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
      Given a customer has an empty cart
      And product "<product>" is in stock
      When the customer adds "<product>" to the cart
      Then the customer sees "<product>" in the cart

      Examples:
        | product      |
        | Travel mug   |
        | Coffee beans |
        | Tea towel    |
        | Notebook     |
```

These products are otherwise equivalent for this rule: each is in stock and can be added
to a cart. Changing their names repeats the same business case, and the table has no descriptive
title.

## Good

```gherkin
Feature: Cart management

  Rule: Products in stock can be added to a cart
    Scenario: A customer adds an available product
      Given a customer has an empty cart
      And product "Travel mug" is in stock
      When the customer adds "Travel mug" to the cart
      Then the customer sees "Travel mug" in the cart
```

Use an Outline when the rows contribute distinct cases:

```gherkin
Feature: Shipping charges

  Rule: Standard shipping costs 5 EUR below 50 EUR and is free from 50 EUR
    Scenario Outline: Shipping charge at the free-shipping threshold
      Given a customer has a cart totaling <total> EUR
      When the customer chooses standard shipping
      Then the customer sees a shipping charge of <charge> EUR

      Examples: Cart totals around the free-shipping threshold
        | total | charge |
        | 49    | 5      |
        | 50    | 0      |
        | 51    | 0      |
```

These rows distinguish below, equal to, and above the threshold while keeping the same steps.

## Sources

- [Automation Panda, BDD 101: Writing good Gherkin](https://automationpanda.com/2017/01/30/bdd-101-writing-good-gherkin/)
