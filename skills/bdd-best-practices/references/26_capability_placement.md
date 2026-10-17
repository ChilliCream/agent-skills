# Rule 26: Place a scenario by its capability

> Put a scenario in the feature of the capability it is about, not of the screen it uses.

[All rules](../SKILL.md)

## Why

Screens move and change names, but capabilities stay. A reader looks for publishing under
`publication/`, not under the page where the publish button is.

For example, attribute templates are managed on a screen under Account, but exporting them is a
data-exchange capability. So the scenario goes in `data-exchange/attribute-templates.feature`. The
`specs/` tree in the sync-autotest specification guide lists all capabilities.

## Bad

```text
specs/account/attribute-template-export.feature    named for the screen
specs/super-admin/subscriptions.feature            named for a role
specs/tc-16457/create-subscription.feature         named for a test case
```

## Good

```text
specs/data-exchange/attribute-templates.feature
specs/subscriptions/management.feature
```

## Sources

- sync-autotest specification guide, _The `specs/` tree_
