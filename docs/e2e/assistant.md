# Assistant E2E

`e2e/diet-planner/assistant.spec.ts` — epic #634 acceptance (#650).

**Journey:** open the assistant panel, ask for a week, confirm the preview card, close the
panel, see the meals in the planner calendar; the API confirms seven planned lunches and none
marked eaten.

**Backend setup:** the spec needs no model provider. Start the API with

```bash
Assistant__Enabled=true Assistant__Provider=Fake Mcp__ImportsEnabled=true
```

(already in `$run-project` and `.github/workflows/e2e-nightly.yml`). `Fake` is a scripted
`IChatClient` (`src/Modules/Assistant/Assistant.Api/FakeChatClient.cs`), refused in
Production. For every user message it:

1. calls `preview_plan_import` with a self-contained plan: one product, one recipe named after
   the message, lunch on each of the seven days from today (server UTC date);
2. when the preview returns a `previewToken`, proposes `execute_plan_import` with it; the
   server holds that write as a pending action, so the panel shows the confirm card;
3. after Confirm, answers "Done. The week is in your planner." (after Cancel, "Okay, nothing
   was planned.").

The spec names the recipe `Assistant week <timestamp>`, so parallel workers and reruns never
collide. Global teardown purges the planned meals with the rest of the worker's data.
