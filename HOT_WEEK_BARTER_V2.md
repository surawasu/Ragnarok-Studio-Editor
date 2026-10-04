# MAVEN Hot Week / Secure Barter V2

MAVEN extends the normal rAthena BARTER_DB format with optional Hot Week fields. Existing barter YAML that does not use these fields keeps the legacy behavior.

## Shop fields

```yaml
- Name: MAVEN_HOT_WEEK
  EventId: HOT_WEEK_202610
  Start: "2026-10-04 20:00:00"
  End: "2026-10-11 23:59:59"
```

- `EventId`: purchase-limit namespace. Change it for a new campaign to start fresh counters.
- `Start` / `End`: local map-server time. Format `YYYY-MM-DD HH:MM[:SS]`.
- A scheduled/event-scoped shop uses Secure V2 even when every item is 100% success.

## Item fields

```yaml
Items:
  - Index: 0
    Item: Red_Potion
    Stock: 100
    SuccessRate: 7500
    ConsumeOnFail: true
    AccountLimit: 3
    CharacterLimit: 1
    Zeny: 10000
    RequiredItems:
      - Index: 0
        Item: Jellopy
        Amount: 10
```

- `Stock: 0` = unlimited. Limited stock remains persisted in the existing `barter` SQL table.
- `SuccessRate`: 0..10000. 10000 = 100%, 7500 = 75%, 1 = 0.01%.
- `ConsumeOnFail: true`: failed chance consumes configured Zeny/requirements.
- `ConsumeOnFail: false`: failed chance is a free retry; Studio warns because this can be abused.
- `AccountLimit: 0` / `CharacterLimit: 0` = unlimited.
- Limits and stock count successful rewards only. Chance failures do not consume stock or success limits.
- Secure V2 accepts one reward unit per purchase request so stock, limit reservation, rollback and audit stay deterministic.

## Purchase safety

Secure V2 re-checks the schedule at purchase time, validates requirements before mutation, atomically reserves success limits and stock in SQL, and rolls reservations/costs back if reward delivery fails.

The account/character counters use conditional SQL updates so simultaneous requests cannot exceed a configured limit. Limited stock uses a conditional SQL decrement (`amount >= requested`) to prevent last-stock oversell.

## Audit and persistence

The server creates/uses:
- `maven_barter_account_usage`
- `maven_barter_character_usage`
- `maven_barter_audit`

Audit rows contain event/shop/item, account/character, requested/success amount, success rate, failure policy, result, stock before/after and timestamp.

For existing databases, migration SQL is available at:

`sql-files/upgrades/upgrade_20261004_maven_barter_hotweek.sql`

The map-server also uses `CREATE TABLE IF NOT EXISTS` when the Barter DB finishes loading as a deployment safeguard.

## Studio workflow

1. Open **Barter Shop Editor • Hot Week / Secure V2**.
2. Load a BARTER_DB YAML file.
3. Select an item row, or use **+ Item / Clone / Delete Item / ↑ Item / ↓ Item** to manage rewards.
4. Edit Reward Item (AegisName), Zeny, Event ID, Start/End, Stock, SuccessRate, ConsumeOnFail and limits.
5. Manage the crafting recipe with **+ Material / Apply Material / Delete Material / ↑ Material / ↓ Material**. RequiredItems indices are maintained automatically.
6. Click **Apply to selected** to update the reward and Hot Week fields.
7. Click **Validate V2** and resolve warnings, including duplicate/missing recipe indices and invalid material amounts.
8. Save. Studio validates YAML and creates a backup before replacing the file.

Reward Item indices are intentionally not renumbered when deleting/reordering existing entries because the persistent barter stock table is keyed by shop + item index. New rewards receive the next free index.

Project Validator also reports schedule/rate/index/stock/free-retry issues and unknown Aegis references.
