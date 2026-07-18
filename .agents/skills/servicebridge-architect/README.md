# ServiceBridge Architect Skill

Place the `servicebridge-architect` folder at this repository-relative path:

```text
.agents/skills/servicebridge-architect
```

Expected structure:

```text
ServiceBridge/
└── .agents/
    └── skills/
        └── servicebridge-architect/
            ├── SKILL.md
            ├── README.md
            └── references/
                ├── current-state.md
                ├── task-workflows.md
                └── prompt-examples.md
```

Start Codex from the repository or a child directory. Invoke explicitly with:

```text
$servicebridge-architect <task>
```

Example:

```text
$servicebridge-architect Inventory Foundation را بر اساس repository فعلی پیاده‌سازی کن. Commit نکن.
```

The canonical project guideline is `docs/ServiceBridge.ProjectGuideline.md`.
The main `SKILL.md` intentionally stays compact. Detailed workflow guidance is
loaded from the remaining `references` files only when needed. Start a new
Codex session after installing or replacing the skill if it is not detected
immediately.
