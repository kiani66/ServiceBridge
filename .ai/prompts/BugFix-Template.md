# Bug Fix Template

## Symptom

Describe the observed failure.

## Root Cause

Trace the real code path and explain the underlying defect.

## Fix Scope

Describe the narrow fix.

## Out of Scope

List related issues not fixed here.

## Validation

Run the relevant focused test, then:

- `dotnet build ServiceBridge.sln`
- `dotnet test ServiceBridge.sln`
