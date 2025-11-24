# UI Design - Admin Dialogs
**Last Updated**: November 2, 2025

## Visual Layout

### Desktop (800px width)

```
┌─────────────────────────────────────────────────────────────┐
│ Manage Concepts                                      [X]     │
├─────────────────────────────────────────────────────────────┤
│ [🔍 Search concepts...]  [Category ▼]  [Sort: Name ▼]      │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ 🟢 Response                               [Edit] [Del]  │ │
│ │    No description                                        │ │
│ ├─────────────────────────────────────────────────────────┤ │
│ │ 🟡 Question                               [Edit] [Del]  │ │
│ │    A query requiring an answer                          │ │
│ ├─────────────────────────────────────────────────────────┤ │
│ │ 🟢 Inspector                              [Edit] [Del]  │ │
│ │    Person conducting inspection                         │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                              │
│ Showing 3 of 11 concepts                                    │
└─────────────────────────────────────────────────────────────┘
```

### Expanded Item (Edit Mode)

```
┌─────────────────────────────────────────────────────────────┐
│ 🟢 Response                                                  │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ Name: [Response________________________]                │ │
│ │ Desc: [A reply to a question or reques_]                │ │
│ │ Cat:  [Communication__________________▼]                │ │
│ │                                    [Cancel] [Save]       │ │
│ └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ 🟡 Question                               [Edit] [Del]      │
└─────────────────────────────────────────────────────────────┘
```

### Mobile (<768px)

```
┌─────────────────────────┐
│ Manage Concepts    [X]  │
├─────────────────────────┤
│ [🔍 Search...]          │
│ [Category ▼]            │
│ [Sort: Name ▼]          │
├─────────────────────────┤
│ 🟢 Response             │
│    No description       │
│    [Edit] [Delete]      │
├─────────────────────────┤
│ 🟡 Question             │
│    A query...           │
│    [Edit] [Delete]      │
└─────────────────────────┘
```

## Color Scheme

### Status Indicators
- 🟢 Green (`--bs-success`) - Valid, no issues
- 🟡 Yellow (`--bs-warning`) - Has warnings
- 🔴 Red (`--bs-danger`) - Has errors
- 🔵 Blue (`--bs-info`) - Orphaned/isolated

### Action Buttons
- Edit: `btn-outline-primary` (blue)
- Delete: `btn-outline-danger` (red)
- Save: `btn-success` (green)
- Cancel: `btn-secondary` (gray)

### Background Colors
- Header: `bg-light` (light mode) / `bg-dark` (dark mode)
- List item: `list-group-item` (Bootstrap default)
- Expanded item: `bg-light` / `bg-dark` with subtle border
- Hover: Slight darkening effect

## Typography

### Font Sizes
- Dialog title: `1.25rem` (20px)
- Search input: `1rem` (16px)
- Entity name: `1rem` (16px), `font-weight: 600`
- Entity description: `0.875rem` (14px), `text-muted`
- Result count: `0.875rem` (14px), `text-muted`
- Badges: `0.75rem` (12px)

### Font Families
- Follow Bootstrap defaults
- Use `monospace` for IDs/URIs

## Spacing

### Padding
- Dialog header: `1rem` (16px)
- Dialog body: `1rem` (16px)
- List item: `0.75rem` (12px) vertical, `1rem` horizontal
- Edit form: `1rem` (16px) all sides
- Button groups: `0.5rem` (8px) gap

### Margins
- Between search and list: `1rem` (16px)
- Between list items: `0` (use borders)
- Between form fields: `0.75rem` (12px)

## Animations

### Dialog Entry/Exit
```css
@keyframes slideInRight {
  from { transform: translateX(100%); opacity: 0; }
  to { transform: translateX(0); opacity: 1; }
}

.admin-dialog {
  animation: slideInRight 200ms ease-out;
}
```

### Item Expand/Collapse
```css
.entity-edit-form {
  max-height: 0;
  overflow: hidden;
  transition: max-height 200ms ease-out;
}

.entity-list-item.expanded .entity-edit-form {
  max-height: 300px;
}
```

### Hover Effects
```css
.entity-list-item {
  transition: background-color 150ms ease;
}

.entity-list-item:hover {
  background-color: rgba(0, 0, 0, 0.02);
}

[data-bs-theme="dark"] .entity-list-item:hover {
  background-color: rgba(255, 255, 255, 0.05);
}
```

## Responsive Breakpoints

### Desktop (≥768px)
- Dialog width: `800px`
- Positioned: `right: 20px`
- Header: Horizontal layout (search, filter, sort in row)

### Tablet (≥576px, <768px)
- Dialog width: `90vw`
- Positioned: `right: 5vw`
- Header: 2 rows (search on row 1, filters on row 2)

### Mobile (<576px)
- Dialog width: `100vw`
- Positioned: `right: 0`
- Header: Stack vertically (3 rows)
- Full-screen modal with slide-up animation

## Icon Usage

### Bootstrap Icons
- Search: `bi-search`
- Edit: `bi-pencil-square`
- Delete: `bi-trash`
- Close: `bi-x`
- Sort: `bi-sort-alpha-down`
- Filter: `bi-funnel`
- Expand: `bi-chevron-down`
- Collapse: `bi-chevron-up`
- Success: `bi-check-circle-fill`
- Error: `bi-exclamation-circle-fill`
- Warning: `bi-exclamation-triangle-fill`
- Info: `bi-info-circle-fill`

## Accessibility

### ARIA Labels
```html
<button aria-label="Edit concept Response">
  <i class="bi bi-pencil-square"></i>
</button>

<input
  type="search"
  placeholder="Search concepts..."
  aria-label="Search concepts"
/>

<div role="list" aria-label="Concept list">
  <div role="listitem">...</div>
</div>
```

### Focus Management
- Dialog opens: Focus search input
- Dialog closes: Return focus to trigger button
- Keyboard nav: Tab through items, Enter to expand/collapse
- Escape: Close dialog or collapse expanded item

### Screen Reader Support
```html
<div aria-live="polite" aria-atomic="true">
  Showing 3 of 11 concepts
</div>

<button aria-expanded="false" aria-controls="entity-edit-5">
  Edit
</button>
```

## CSS Class Naming

Following BEM convention:

```css
/* Block */
.admin-dialog { }

/* Elements */
.admin-dialog__header { }
.admin-dialog__search { }
.admin-dialog__filters { }
.admin-dialog__body { }
.admin-dialog__footer { }

/* Entity list */
.entity-list { }
.entity-list__item { }
.entity-list__item-header { }
.entity-list__item-body { }
.entity-list__item-actions { }

/* Modifiers */
.entity-list__item--expanded { }
.entity-list__item--error { }
.entity-list__item--warning { }
```

## Example HTML Structure

```html
<div class="admin-dialog">
  <div class="admin-dialog__header">
    <h5>Manage Concepts</h5>
    <button class="btn-close" aria-label="Close"></button>
  </div>

  <div class="admin-dialog__filters">
    <input class="form-control" placeholder="Search..." />
    <select class="form-select">...</select>
  </div>

  <div class="admin-dialog__body">
    <div class="entity-list">
      <div class="entity-list__item">
        <div class="entity-list__item-header">
          <span class="badge bg-success">✓</span>
          <strong>Response</strong>
          <div class="entity-list__item-actions">
            <button class="btn btn-sm btn-outline-primary">
              <i class="bi bi-pencil-square"></i> Edit
            </button>
            <button class="btn btn-sm btn-outline-danger">
              <i class="bi bi-trash"></i>
            </button>
          </div>
        </div>
        <div class="entity-list__item-body text-muted">
          No description
        </div>
      </div>
    </div>
  </div>

  <div class="admin-dialog__footer text-muted">
    Showing 3 of 11 concepts
  </div>
</div>
```

## Dark Mode Support

```css
/* Light mode (default) */
.admin-dialog {
  background-color: var(--bs-body-bg);
  color: var(--bs-body-color);
  border: 1px solid var(--bs-border-color);
}

/* Dark mode */
[data-bs-theme="dark"] .admin-dialog {
  background-color: var(--bs-dark);
  border-color: var(--bs-secondary);
}

[data-bs-theme="dark"] .entity-list__item {
  border-color: var(--bs-secondary);
}

[data-bs-theme="dark"] .entity-list__item:hover {
  background-color: rgba(255, 255, 255, 0.05);
}
```

## Loading States

### Initial Load
```html
<div class="admin-dialog__body">
  <div class="text-center py-5">
    <div class="spinner-border text-primary" role="status">
      <span class="visually-hidden">Loading...</span>
    </div>
    <p class="text-muted mt-2">Loading concepts...</p>
  </div>
</div>
```

### Empty State
```html
<div class="admin-dialog__body">
  <div class="text-center py-5 text-muted">
    <i class="bi bi-inbox" style="font-size: 3rem;"></i>
    <p class="mt-3">No concepts found</p>
    <button class="btn btn-primary btn-sm">
      Add First Concept
    </button>
  </div>
</div>
```

### No Search Results
```html
<div class="text-center py-4 text-muted">
  <i class="bi bi-search" style="font-size: 2rem;"></i>
  <p class="mt-2">No results for "xyz"</p>
  <button class="btn btn-link btn-sm">Clear search</button>
</div>
```

---
**Next**: See [implementation-plan.md](./implementation-plan.md) for step-by-step tasks
