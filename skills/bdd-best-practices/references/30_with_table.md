# Rule 30: Use tables for coherent business facts

> Use a data table when it makes one structured business fact or collection easier to read. Keep
> simple values in the step sentence and keep behavior explicit in the steps.

[All rules](../SKILL.md)

## Why

Data tables can describe starting state in a `Given`, structured input to one action in a `When`,
or an expected collection in a `Then`. Choose the form that explains the business rule most
clearly; the step keyword does not determine whether a table is appropriate.

A table works when its rows describe related facts and its headings use business language.
For one product name or one quantity, a sentence is usually clearer. Keep unrelated setup
facts in separate steps, and express actions as steps rather than as commands hidden in table
cells. `Examples` tables serve a different purpose: rules 28 and 29 explain them.

## Bad

```gherkin
Feature: Cart management

  Rule: Products in stock can be added to a cart
    Scenario: A customer adds an available product
      Given these settings:
        | customer   | product    | stock    | action      | expected result |
        | a customer | Travel mug | in stock | add to cart | product in cart |
      When the settings are processed
      Then the expected result is correct
```

The table mixes state, an action, and an assertion. The steps no longer explain the behavior.

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

A short sentence carries each simple fact. Use tables when the structure itself matters:

```gherkin
Feature: Cart totals

  Rule: The cart total is the sum of its product quantities multiplied by their prices
    Scenario: A customer adds several products in one action
      Given a customer has an empty cart
      And these products are in stock:
        | product      | unit price |
        | Travel mug   | 10 EUR     |
        | Coffee beans | 5 EUR      |
      When the customer adds these products to the cart together:
        | product      | quantity |
        | Travel mug   | 2        |
        | Coffee beans | 1        |
      Then the customer sees these products in the cart:
        | product      | quantity | line total |
        | Travel mug   | 2        | 20 EUR     |
        | Coffee beans | 1        | 5 EUR      |
      And the customer sees a cart total of 25 EUR
```

Each table has one purpose: product prices, the input to a single action, or the resulting cart
contents. The action remains explicit. Use the `When` table only if adding several products
together is a real business operation in the webshop; otherwise set up the cart contents as
starting state or describe each independent addition in its own scenario.

## Sources

- [Cucumber, Gherkin reference](https://cucumber.io/docs/gherkin/reference/)
