# Dialogs

## Types

- Confirmation.
- Delete.
- Edit.
- Information.

## Rules

- Dialogs are for blocking decisions or focused edits, not general page layout.
- Titles must state the decision or object being edited.
- Body copy should be brief and specific.
- Delete dialogs must identify what will be deleted and whether the action can be undone.
- Primary action appears last in reading order when buttons are horizontal.
- Dialogs must trap focus, close with Escape when safe, and restore focus to the triggering control.

## Sizing

| Size | Width | Usage |
| --- | ---: | --- |
| Small | 400px | Confirmation or delete |
| Medium | 560px | Edit or information |
| Large | 720px | Complex edit with multiple fields |

## States

- Default.
- Loading.
- Validation error.
- Submission error.
