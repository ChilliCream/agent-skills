# Rule 16: Authentication is setup unless it is the behaviour

> State authentication as a `Given` when it supports another behaviour. Use a `When` for signing
> in or out when authentication is the behaviour under test.

[All rules](../SKILL.md)

## Why

Signing in before checking order history describes how the starting state was reached. The
business behaviour is viewing the customer's orders, so write `the customer is signed in` as a
starting fact. The step implementation establishes the authenticated session in the way the
application supports. Naming an actor does not itself authenticate that actor.

When a scenario tests signing in or signing out, authentication is the act under test. Keep that
act as the `When` and check its visible result.

## Bad

```gherkin
Feature: Order history

  Rule: Signed-in customers can see their orders
    Scenario: A customer views an existing order
      Given a customer has an order
      When the customer signs in
      And the customer views the order history
      Then the customer sees the order in the order history
```

## Good

```gherkin
Feature: Order history

  Rule: Signed-in customers can see their orders
    Scenario: A customer views an existing order
      Given a customer has an order
      And the customer is signed in
      When the customer views the order history
      Then the customer sees the order in the order history
```

```gherkin
Feature: Customer sign-in

  Rule: Valid credentials give a customer access to their account
    Scenario: A customer signs in with valid credentials
      Given a customer has an account
      And the customer is signed out
      When the customer signs in with valid credentials
      Then the customer sees their account details on the account page
```

Here signing in is the behaviour, so it is the `When`. A sign-out scenario likewise uses
`When the customer signs out` and checks that account access requires authentication again.

## Sources

- [Cucumber, Writing better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/)
